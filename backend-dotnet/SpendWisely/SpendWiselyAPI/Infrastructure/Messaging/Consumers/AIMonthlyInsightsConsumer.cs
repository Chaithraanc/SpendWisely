using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SpendWiselyAPI.Application.DTOs.AIInsights;
using SpendWiselyAPI.Application.DTOs.Budget;
using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.AI;
using SpendWiselyAPI.Infrastructure.Caching.MonthlySummary;
using SpendWiselyAPI.Infrastructure.Mappers;
using SpendWiselyAPI.Infrastructure.Models;
using System.Text;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.Messaging.Consumers
{
    public class AIMonthlyInsightsConsumer : BackgroundService
    {
        private readonly ILogger<AIMonthlyInsightsConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAIService _aiService;
        private readonly MessagingPolicies _policies;
        private readonly IRedisService _redis;

        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private const string MainExchange = "spendwisely.events";
        private const string MainQueue = "spendwisely.ai.monthly";

        private const string RetryExchange = "spendwisely.ai.monthly.retry.exchange";
        private const string RetryQueue = "spendwisely.ai.monthly.retry";

        private const string DlqExchange = "spendwisely.ai.monthly.dlq.exchange";
        private const string DlqQueue = "spendwisely.ai.monthly.dlq";

        private const int MaxRetries = 3;

        public AIMonthlyInsightsConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<AIMonthlyInsightsConsumer> logger,
            IAIService aiService,
            MessagingPolicies policies
            )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _aiService = aiService;
            _policies = policies;


            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            ConfigureQueues();
        }

        private void ConfigureQueues()
        {
            // MAIN QUEUE
            _channel.QueueDeclareAsync(
                queue: MainQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                { "x-dead-letter-exchange", RetryExchange },
                { "x-dead-letter-routing-key", "retry" }
                }).GetAwaiter().GetResult();

            _channel.QueueBindAsync(MainQueue, MainExchange, "monthly.summary.generated")
                .GetAwaiter().GetResult();

            // RETRY QUEUE
            _channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: RetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                { "x-message-ttl", 10000 },
                { "x-dead-letter-exchange", MainExchange },
                { "x-dead-letter-routing-key", "monthly.summary.generated" }
                }).GetAwaiter().GetResult();

            _channel.QueueBindAsync(RetryQueue, RetryExchange, "retry").GetAwaiter().GetResult();

            // DLQ
            _channel.ExchangeDeclareAsync(DlqExchange, ExchangeType.Direct).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: DlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBindAsync(DlqQueue, DlqExchange, "dlq").GetAwaiter().GetResult();

            _channel.BasicQosAsync(0, 1, false);

            _logger.LogInformation("Queues configured: {MainQueue}, {RetryQueue}, {DlqQueue}", MainQueue, RetryQueue, DlqQueue);
        }

        protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting AIMonthlyInsightsConsumer...");
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var evt = JsonSerializer.Deserialize<MonthlySummaryGeneratedEvent>(json);
                _logger.LogInformation("Received MonthlySummaryGeneratedEvent: UserId={UserId}, Year={Year}, Month={Month}",
                    evt?.UserId, evt?.Year, evt?.Month);
                if (evt == null)
                {
                    _logger.LogWarning("Received null MonthlySummaryGeneratedEvent. Acknowledging message.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                try
                {
                    _logger.LogInformation("Processing MonthlySummaryGeneratedEvent for UserId={UserId}, Year={Year}, Month={Month}",
                        evt.UserId, evt.Year, evt.Month);
                    await ProcessMonthlySummaryAsync(evt, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogInformation("Successfully processed MonthlySummaryGeneratedEvent for UserId={UserId}, Year={Year}, Month={Month}",
                        evt.UserId, evt.Year, evt.Month);
                }
                catch (Exception ex)
                {
                    int retryCount = GetRetryCount(ea.BasicProperties);

                    if (retryCount >= MaxRetries)
                    {
                        await _channel.BasicPublishAsync(
                            exchange: DlqExchange,
                            routingKey: "dlq",
                            body: body);

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    var retryProps = new BasicProperties
                    {
                        Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", retryCount + 1 }
                    }
                    };

                    await _channel.BasicPublishAsync(
                        exchange: RetryExchange,
                        routingKey: "retry",
                        basicProperties: retryProps,
                        mandatory: true,
                        body: body);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync(MainQueue, autoAck: false, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessMonthlySummaryAsync(
            MonthlySummaryGeneratedEvent evt,
            CancellationToken token)
        {

            using var scope = _scopeFactory.CreateScope();

            var summaryRepo = scope.ServiceProvider.GetRequiredService<IDashboardMonthlySummaryRepository>();
            var budgetRepo = scope.ServiceProvider.GetRequiredService<IBudgetRepository>();
            var insightsRepo = scope.ServiceProvider.GetRequiredService<IAIInsightsRepository>();
            var _processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();

            // Idempotency
            if (await _processedRepo.ExistsAsync(evt.EventId, evt.EventType))
            {
                _logger.LogInformation("Event {EventId} already Processed and AIInsights generated for the month {Month} and Year {Year}", evt.EventId, evt.Month, evt.Year);
                return;
            }

            _logger.LogInformation("Loading data for AI insights generation for UserId={UserId}, Year={Year}, Month={Month}",
               evt.UserId, evt.Year, evt.Month);

            // Load current month summary for all users (AI will filter by UserId)
            var currentDto = await summaryRepo.GetMonthlySummaryAsync(evt.Year, evt.Month);


            // Load history for all users(all rows for the year)
            var historyDto = await summaryRepo.GetYearlySummaryAsync(evt.Year);



            // Load budgets for the year for all users
            var budgets = await budgetRepo.GetBudgetsByYearAsync(evt.Year);
            var budgetsDto = budgets.Select(b => b.ToDto()).ToList();
            // Get distinct userIds across all datasets
            var users = currentDto
      .Select(x => x.UserId)
      .Union(historyDto.Select(x => x.UserId))
      .Union(budgetsDto.Select(x => x.UserId))
      .Distinct()
      .ToList();
            // Prepare AI input for each user
            var aiInputs = new List<AIInsightsInput>();

            foreach (var userId in users)
            {
                var userCurrent = currentDto
                    .Where(x => x.UserId == userId)
                    .ToList();

                var userHistory = historyDto
                    .Where(x => x.UserId == userId)
                    .ToList();

                var userBudgets = budgetsDto
                    .Where(x => x.UserId == userId)
                    .ToList();

                aiInputs.Add(new AIInsightsInput
                {
                    UserId = userId,
                    Month = evt.Month,
                    Year = evt.Year,
                    CurrentMonthSummary = userCurrent,
                    HistoricalSummaries = userHistory,
                    BudgetAllocations = userBudgets
                });
            }
            //Prepare batch input for AI service
            var batch = new AIInsightsBatchInput
            {
                Users = aiInputs
            };


            _logger.LogInformation("Calling AI service for UserId={UserId}, Year={Year}, Month={Month}",
                evt.UserId, evt.Year, evt.Month);


            // Call OpenAI with retry + circuit breaker
            // Note: We call AI service once per month with batch input for all users to optimize costs and performance.
            //returns Insights per user in one Json array with userId
            var result = await _policies.RetryPolicy
                .WrapAsync(_policies.CircuitBreakerPolicy)
                .ExecuteAsync(async () =>
                {
                    return await _aiService.GenerateMonthlyInsightsBatchAsync(batch);
                });
            _logger.LogInformation("Received AI insights, Year={Year}, Month={Month}Result: {Result}",
                evt.Year, evt.Month, JsonSerializer.Serialize(result));

            // Deserialize the result into a list of AIInsightsResponseDto
          //  var insightsList = JsonSerializer.Deserialize<List<AIInsightsResultDto>>(result);
              var insightsList = JsonSerializer.Deserialize<List<AIInsightsResultDto>>(
              result,
              new JsonSerializerOptions
              {
                PropertyNameCaseInsensitive = true
              });
            // Store insights for each user and emit event
            foreach (var insight in insightsList)
            {
                // Convert this user's insight object back to JSON string
                var json = JsonSerializer.Serialize(insight);
                AIInsights aiInsights;
                // Insert or update
                if (await insightsRepo.CheckExistsAIInsightsAsync(insight.UserId, evt.Year, evt.Month))
                {
                    _logger.LogInformation(
                        "AI insights already exist for UserId={UserId}, Year={Year}, Month={Month}. Updating existing record.",
                        insight.UserId, evt.Year, evt.Month);

                    aiInsights = await insightsRepo.GetAIInsightsAsync(insight.UserId, evt.Year, evt.Month);
                   
                    aiInsights.UpdateInsights(json);
                    
                }
                else
                {
                    aiInsights = new AIInsights(
                        insight.UserId,
                        evt.Year,
                        evt.Month,
                        json
                    );

                    await insightsRepo.InsertAIInsightsAsync(aiInsights);
                }

                // Emit event per user

                var generatedEvent = new AIInsightGeneratedEvent
                {
                    EventId = Guid.NewGuid(),
                    AggregateId = aiInsights.Id,
                    EventType = "AIInsightGenerated",
                    UserId = aiInsights.UserId,
                    Year = aiInsights.Year,
                    Month = aiInsights.Month,
                    Insights = json,
                    Timestamp = DateTime.UtcNow

                };
                //publish event to RabbitMQ for each user
                await _channel.BasicPublishAsync(
                   exchange: MainExchange,
                   routingKey: "ai.insight.generated",
                   mandatory: true,
                   // basicProperties: null,
                   body: JsonSerializer.SerializeToUtf8Bytes(generatedEvent));

                _logger.LogInformation("AI insights stored and event published for UserId={UserId}, Year={Year}, Month={Month}",
              aiInsights.UserId, aiInsights.Year, aiInsights.Month);
            }

            await insightsRepo.SaveChangesAsync(token);
          //  await _redis.SetMonthlyAIInsightsGeneratedAsync(evt.Year, evt.Month);
        }

    


        private int GetRetryCount(IReadOnlyBasicProperties props)
        {
            if (props?.Headers != null &&
                props.Headers.TryGetValue("x-retry-count", out var value))
            {
                return Convert.ToInt32(value);
            }
            return 0;
        }
    }
}
