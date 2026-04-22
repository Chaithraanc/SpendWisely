using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Events.Models;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class ProcessedEventsRepository : IProcessedEventsRepository
    {
        private readonly AppDbContext _context;

        public ProcessedEventsRepository(AppDbContext context)
        {
            _context = context;
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            return _context.Database.BeginTransactionAsync(ct);
        }
        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _context.SaveChangesAsync(ct);
        }
        public async Task<bool> ExistsAsync(Guid eventId ,string eventType )
        {
            return await _context.ProcessedEvents
                .AnyAsync(e => e.EventId == eventId && e.EventType == eventType);
        }

        public async Task MarkProcessedAsync(Guid eventId, Guid? aggregateId, string eventType,  Guid? userId, string payload)
        {
            var entity = new ProcessedEvent
            {
                EventId = eventId,
                EventType = eventType,
                AggregateId = aggregateId,
                UserId = userId,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };

            _context.ProcessedEvents.Add(entity);
            //await _context.SaveChangesAsync();
        }
    }
}

