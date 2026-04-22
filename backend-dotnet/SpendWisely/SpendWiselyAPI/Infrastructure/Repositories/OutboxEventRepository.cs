using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class OutboxEventRepository : IOutboxEventRepository
    {
        private readonly AppDbContext _dbContext;

        public OutboxEventRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            return _dbContext.Database.BeginTransactionAsync(ct);
        }

        //cacellation token is not needed here because we are just adding an event to the db context, the actual saving happens in the unit of work pattern where the cancellation token will be used.
        public Task AddOutboxEventAsync(OutboxEvent outboxEvent)
        {
            _dbContext.OutboxEvents.Add(outboxEvent);
            return Task.CompletedTask;
           
        }
        //cancellation token is needed here because we are querying the database for unprocessed events, which can be a long-running operation and we want to be able to cancel it if needed.
        public async Task<IReadOnlyList<OutboxEvent>> GetUnprocessedEventsAsync(int BatchSize, CancellationToken ct)
        {
            return await _dbContext.OutboxEvents
                .Where(e => !e.Processed && e.RetryCount <=5)
                .OrderBy(e => e.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);
        }
        public async Task MarkAsProcessedAsync(Guid eventId, CancellationToken ct)
        {
            var evt = await _dbContext.OutboxEvents.FindAsync(new object[] { eventId }, ct);
            if (evt == null) return;

            evt.Processed = true;
            evt.PublishedAtUTC = DateTime.UtcNow;

         
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _dbContext.SaveChangesAsync(ct);
        }


        public async Task<int> IncrementRetryAsync(Guid id, string error, CancellationToken ct)
        {
            var evt = await _dbContext.OutboxEvents.FindAsync(new object[] { id }, ct);
            if (evt == null) return 0;

            evt.RetryCount += 1;
            evt.LastErrorMessage = error;
            evt.ScheduledAtUTC = DateTime.UtcNow.AddSeconds(Math.Pow(2, evt.RetryCount)); // exponential backoff

         

            return evt.RetryCount;
        }

        public async Task MoveToFailedAsync(Guid id, string reason, CancellationToken ct)
        {
            var evt = await _dbContext.OutboxEvents.FindAsync(new object[] { id }, ct);
            if (evt == null) return;

            var failed = new OutboxFailedEvent
            {
                Id = Guid.NewGuid(),
                EventType = evt.EventType,
                AggregateId = evt.AggregateId,
                Payload = evt.Payload,
                RetryCount = evt.RetryCount,
                FailureReason = reason,
                FailedAt = DateTime.UtcNow
            };

            _dbContext.OutboxFailedEvents.Add(failed);
            _dbContext.OutboxEvents.Remove(evt);

           
        }

    }
}
