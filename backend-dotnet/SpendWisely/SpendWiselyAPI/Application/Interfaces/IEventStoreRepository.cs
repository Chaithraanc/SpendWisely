using SpendWiselyAPI.Infrastructure.Events.Models;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IEventStoreRepository
    {
        Task SaveEventAsync<TEvent>(DomainEventEnvelope<TEvent> @event, CancellationToken ct);
        Task<IEnumerable<T>> GetEventsByAggregateIdAsync<T>(Guid aggregateId, CancellationToken ct);
        Task<IEnumerable<T>> GetAllEventsAsync<T>(CancellationToken ct);
    }
}
