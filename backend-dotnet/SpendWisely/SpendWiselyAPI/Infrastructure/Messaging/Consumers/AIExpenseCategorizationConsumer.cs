using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.AI;
using SpendWiselyAPI.Infrastructure.Caching;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.Repositories;
using StackExchange.Redis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpendWiselyAPI.Infrastructure.Messaging.Consumers
{
    public class AIExpenseCategorizationConsumer : BackgroundService
    {
        private readonly ILogger<AIExpenseCategorizationConsumer> _logger;
        private readonly IAIService _aiService;
      
       // private readonly IRedisCache _cache;
       
   
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        

        private const string MainExchange = "spendwisely.events";
        private const string MainQueue = "spendwisely.ai.expense";

        private const string RetryExchange = "spendwisely.ai.expense.retry.exchange";
        private const string RetryQueue = "spendwisely.ai.expense.retry";

        private const string DlqExchange = "spendwisely.ai.expense.dlq.exchange";
        private const string DlqQueue = "spendwisely.ai.expense.dlq";

        private const int MaxRetries = 3;

        
        private readonly MessagingPolicies _policies;
        private readonly IDatabase _redis;

        public AIExpenseCategorizationConsumer(
            IServiceScopeFactory scopeFactory,
       
            ILogger<AIExpenseCategorizationConsumer> logger,
            IAIService aiService,
          
           // IRedisCache cache,
            IConnectionMultiplexer muxer,
        
            MessagingPolicies policies)
        {
            _scopeFactory = scopeFactory;
          
           
            _logger = logger;
            _aiService = aiService;
          
            //_cache = cache;
         
            _policies = policies;
            _redis = muxer.GetDatabase();



            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        
           
             ConfigureQueues();

           

            _policies = policies;

            
        }

        private void ConfigureQueues()
        {
            // MAIN QUEUE (expense created/updated)
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

            _channel.QueueBindAsync(MainQueue, MainExchange, "expense.created").GetAwaiter().GetResult();
            _channel.QueueBindAsync(MainQueue, MainExchange, "expense.updated").GetAwaiter().GetResult();

            // RETRY QUEUE (10s delay)
            _channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: RetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    { "x-message-ttl", 10000 }, // 10 seconds
                    { "x-dead-letter-exchange", MainExchange },
                    { "x-dead-letter-routing-key", "expense.ai" }
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
            _logger.LogInformation("RabbitMQ queues and exchanges configured successfully.");
        }

        protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AIExpenseCategorizationConsumer starting up. Waiting for messages...");
            await Task.Delay(2000, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                if (body == null || body.Length == 0)
                {
                    _logger.LogWarning("Received empty message body. Skipping.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }


                var evt = JsonSerializer.Deserialize<ExpenseEvent>(json);
             
                if (evt == null)
                {
                    _logger.LogWarning("Received null or invalid ExpenseEvent payload.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                if (evt.EventType is not ("ExpenseCreated" or "ExpenseUpdated"))
                {
                    _logger.LogInformation("Skipping event type {EventType}", evt.EventType);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                try
                {
                    _logger.LogWarning("calling ProcessExpenseAsync.");
                    await ProcessExpenseAsync(evt, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogInformation("Successfully processed ExpenseId={ExpenseId}, EventId={EventId}",
                        evt.ExpenseId, evt.EventId);
                }
                catch (Exception ex)
                {
                    int retryCount = GetRetryCount(ea.BasicProperties);

                    if (retryCount >= MaxRetries)
                    {
                        _logger.LogError(ex,
                            "Permanent failure for ExpenseId={ExpenseId}, EventId={EventId} → DLQ",
                            evt.ExpenseId,
                            evt.EventId);

                        var properties = new BasicProperties
                        {
                            Headers = null
                        };

                        await _channel.BasicPublishAsync(
                            exchange: DlqExchange,
                            routingKey: "dlq",
                            mandatory: false,
                            basicProperties: properties,
                            body: body);

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    _logger.LogWarning(ex,
                        "Transient AI failure for ExpenseId={ExpenseId}, EventId={EventId} → retry queue. Retry {RetryCount}/{MaxRetries}",
                        evt.ExpenseId,
                        evt.EventId,
                        retryCount + 1,
                        MaxRetries);

                    var retryProperties = new BasicProperties
                    {
                        Headers = new Dictionary<string, object?>
                        {
                            { "x-retry-count", retryCount + 1 }
                        }
                    };

                    await _channel.BasicPublishAsync(
                        exchange: RetryExchange,
                        routingKey: "retry",
                        mandatory: true,
                        basicProperties: retryProperties,
                        body: body);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

           await _channel.BasicConsumeAsync(MainQueue, autoAck: false, consumer);
            _logger.LogInformation("AIExpenseCategorizationConsumer consumed messages from {MainQueue}", MainQueue);
            return Task.CompletedTask;
        }

        private async Task ProcessExpenseAsync(ExpenseEvent evt, CancellationToken stoppingToken)
      {
            using var scope = _scopeFactory.CreateScope();

            var _expenseRepo = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();
            var _eventStore = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();
            var _processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();
        
          
        

            _logger.LogInformation("Processing expense {ExpenseId}", evt.ExpenseId);
            if (evt == null)
            {
                _logger.LogError("AIExpenseCategorizationConsumer: evt is NULL");
                return;
            }
            if (evt.EventId == Guid.Empty || evt.ExpenseId == Guid.Empty)
            {
                _logger.LogError("Invalid event: EventId or ExpenseId is NULL/EMPTY. {@evt}", evt);
                return;
            }



            // Poison message detection
            if (string.IsNullOrWhiteSpace(evt.Description))
            {
                _logger.LogError("Poison message detected. Sending to DLQ. EventId={EventId}, ExpenseId={ExpenseId}",
                    evt.EventId, evt.ExpenseId);

                var body = JsonSerializer.SerializeToUtf8Bytes(evt);

                // No retry for poison messages - send directly to DLQ
                await _channel.BasicPublishAsync(
                    exchange: DlqExchange,
                    routingKey: "dlq",
                    mandatory: false,
                   // basicProperties: null,
                    body: body);

                return;
            }

            // Idempotency
            if (await _processedRepo.ExistsAsync(evt.EventId , evt.EventType))
            {
                _logger.LogInformation("Event {EventId} already categorized", evt.EventId);
                return;
            }
            // Check cache first
            var normalized = Normalize(evt.Description);
            var cacheKey = $"category:desc:{normalized}";
            var category = string.Empty;
            _logger.LogInformation("Checking cache for key {CacheKey}", cacheKey);
            try
            {

                var redisValue = await _redis.StringGetAsync(cacheKey);
                category = redisValue.HasValue ? redisValue.ToString() : null;
                _logger.LogInformation("Cache lookup for key {CacheKey} returned: {Category}", cacheKey, category ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache error for key {CacheKey}", cacheKey);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                _logger.LogInformation("Cache hit for '{Desc}' → {Category}", evt.Description, category);
            }
            
           
            // If not in cache, call AI (with circuit breaker)
            if (string.IsNullOrWhiteSpace(category))
            {
                _logger.LogInformation("Calling AI service for '{Desc}'", evt.Description);
                // Wrap AI call with retry and circuit breaker policies
                category = await _policies.RetryPolicy
                .WrapAsync(_policies.CircuitBreakerPolicy)
                .ExecuteAsync(async () =>
                {
                  return await _aiService.CategorizeExpenseAsync(evt.Description );
                });
                _logger.LogWarning("category returned by AI service." + category);
                if (!IsValidCategory(category))
                {
                    _logger.LogWarning("AI returned invalid category '{Category}' for '{Desc}'. Falling back to 'Other'.",
                        category, evt.Description);
                    category = "Other";
                }

                // Cache the result for future use and reduce AI calls
             
              _redis.StringSet(cacheKey, category, TimeSpan.FromDays(90));
                _logger.LogInformation("Cached category '{Category}' for key {CacheKey}", category, cacheKey);

            }
            //Begin transaction to ensure atomicity of DB updates and event recording
            _logger.LogInformation("Beginning transaction for ExpenseId={ExpenseId}", evt.ExpenseId);
            using var tx = await _expenseRepo.BeginTransactionAsync(stoppingToken);
            //  Update expense with category
            await _expenseRepo.UpdateExpenseCategoryAsync(evt.ExpenseId, category);

            var categorizedEvent = new ExpenseCategorizedEvent
            {
                EventId = Guid.NewGuid(),
                ExpenseId = evt.ExpenseId,
                EventType = "ExpenseCategorized",
                UserId = evt.UserId,
                Category = category,
                Description = evt.Description,
                Amount = evt.Amount,
                Timestamp = DateTime.UtcNow
            };

       
            await _expenseRepo.SaveChangesAsync(stoppingToken);
         
            await tx.CommitAsync(stoppingToken);
           _logger.LogInformation("Transaction committed for ExpenseId={ExpenseId}", evt.ExpenseId);
            // Publish ExpenseCategorized event for other services
            await _channel.BasicPublishAsync(
                exchange: MainExchange,
                routingKey: "expense.categorized",
                mandatory: true,
               // basicProperties: null,
                body: JsonSerializer.SerializeToUtf8Bytes(categorizedEvent));

            _logger.LogInformation("Published ExpenseCategorized event for ExpenseId={ExpenseId}", evt.ExpenseId);
        }

        private static string Normalize(string text)
        {
            text = text.ToLowerInvariant().Trim();
            text = new string(text.Where(c => !char.IsPunctuation(c)).ToArray());
            return Regex.Replace(text, @"\s+", " ");
        }

        private bool IsValidCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            using var scope = _scopeFactory.CreateScope();
            var _categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
             HashSet<string> _allowedCategories = new HashSet<string>(
                   _categoryRepo.GetAllCategoriesAsync().GetAwaiter().GetResult().Select(c => c.Name));

            if (_allowedCategories == null || _allowedCategories.Count == 0)
            {
                _logger.LogWarning("No categories found in DB. Defaulting to Other.");
                return false;
            }

          

            return _allowedCategories.Contains(category);
        }



        public void Dispose()
        {
            if (_channel != null && !_channel.IsClosed)
            {
                _channel.CloseAsync(200, "Disposing", false).GetAwaiter().GetResult();
            }

            _channel?.Dispose();
            _connection?.Dispose();
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
