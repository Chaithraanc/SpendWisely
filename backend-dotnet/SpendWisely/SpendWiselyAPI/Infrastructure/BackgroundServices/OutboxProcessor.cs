using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Services;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.Messaging;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.BackgroundServices
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MessagingPolicies _policies;
        private readonly ILogger<OutboxProcessor> _logger;

        private const int BatchSize = 50;
        private const int MaxRetries = 5;

        public OutboxProcessor(IServiceScopeFactory scopeFactory, MessagingPolicies policies ,ILogger<OutboxProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _policies = policies;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessor started.");
            while (!stoppingToken.IsCancellationRequested)
            {
               // try
               // {
               _logger.LogInformation("Processing outbox batch...");

                await _policies.RetryPolicy
                .WrapAsync(_policies.CircuitBreakerPolicy)
                .ExecuteAsync(async ct =>
                    {
                        await ProcessOutboxBatch(ct);
                    }, stoppingToken);
              //  }
                //catch (BrokenCircuitException)
                //{
                //    // Circuit is open — wait before retrying to allow the external system to recover
                //    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                //}

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);// Short delay between batches to prevent tight loop when there are no events
            }
        }

        private async Task ProcessOutboxBatch(CancellationToken ct)
        {
            _logger.LogInformation("Starting to process outbox batch.");
            // Create a new scope to get fresh instances of the repository and publisher for each batch, ensuring proper disposal and avoiding issues with long-lived services in a background task.
            using var scope = _scopeFactory.CreateScope();

            var repo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var events = await repo.GetUnprocessedEventsAsync(BatchSize, ct);
            _logger.LogInformation("Fetched {Count} unprocessed events from outbox.", events.Count);   
            if (events.Count == 0)
                return;
            // Process each event in a transaction to ensure atomicity of publish and mark as processed
            using var tx = await repo.BeginTransactionAsync(ct);
            _logger.LogInformation("Processing {Count} events in transaction.", events.Count);
            foreach (var evt in events)
            {
                try
                {
                    await _policies.RetryPolicy.ExecuteAsync(async retryCt =>
                    {
                        await PublishEventAsync(evt, publisher, retryCt);
                        _logger.LogInformation("Successfully published event {EventId} of type {EventType}.", evt.Id, evt.EventType);
                    }, ct);

                    await repo.MarkAsProcessedAsync(evt.Id, ct);
                    _logger.LogInformation("Marked event {EventId} as processed.", evt.Id);
                }
                catch (Exception ex)
                {
                    var retryCount = await repo.IncrementRetryAsync(evt.Id, ex.Message, ct);

                    if (retryCount >= MaxRetries)
                    {
                        await repo.MoveToFailedAsync(evt.Id, ex.Message, ct);
                    }
                }
            }

            await repo.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            _logger.LogInformation("Finished processing outbox batch.");
        }

        private async Task PublishEventAsync(OutboxEvent evt, IEventPublisher publisher, CancellationToken ct)
        {
            switch (evt.EventType)
            {
                case "ExpenseCreated":
                case "ExpenseUpdated":
                case "ExpenseDeleted":
                    var expenseEvent = JsonSerializer.Deserialize<ExpenseEvent>(evt.Payload);
                    await publisher.PublishEventAsync(expenseEvent, ct);
                    break;
                case "BudgetCreated":
                case "BudgetUpdated":
                case "BudgetDeleted":
                    var budgetEvent = JsonSerializer.Deserialize<BudgetEvent>(evt.Payload);
                    await publisher.PublishEventAsync(budgetEvent , ct);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown event type: {evt.EventType}");
            }
        }
    }
}
