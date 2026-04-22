using Microsoft.EntityFrameworkCore.Storage;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IProcessedEventsRepository
    {
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<bool> ExistsAsync(Guid eventId ,string eventType );
        Task MarkProcessedAsync(Guid eventId, Guid? aggregateId ,string eventType,  Guid? userId, string payload);
    }
}
