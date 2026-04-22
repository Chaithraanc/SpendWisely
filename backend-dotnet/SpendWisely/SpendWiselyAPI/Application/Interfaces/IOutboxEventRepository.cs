using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Infrastructure.Events.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IOutboxEventRepository
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task<IReadOnlyList<OutboxEvent>> GetUnprocessedEventsAsync( int BatchSize , CancellationToken ct);
        Task MarkAsProcessedAsync(Guid eventId , CancellationToken ct);
        Task AddOutboxEventAsync(OutboxEvent outboxEvent);
        Task<int> IncrementRetryAsync(Guid id, string error, CancellationToken ct);
        Task MoveToFailedAsync(Guid id, string reason, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);

    }



}
