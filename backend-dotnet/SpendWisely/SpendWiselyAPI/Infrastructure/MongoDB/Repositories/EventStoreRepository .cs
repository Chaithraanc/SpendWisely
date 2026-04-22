using MongoDB.Bson;
using MongoDB.Driver;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.Events.Models;
using SpendWiselyAPI.Infrastructure.MongoDB;
using SpendWiselyAPI.Infrastructure.MongoDB.Documents;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.MongoDB.Repositories
{
    public class EventStoreRepository :IEventStoreRepository
    {
        private readonly IMongoCollection<EventDocument> _collection;

        public EventStoreRepository(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _collection = database.GetCollection<EventDocument>(settings.EventsCollectionName);
        }

        public async Task SaveEventAsync<TEvent>(DomainEventEnvelope<TEvent> envelope, CancellationToken ct)
        {
            var doc = new EventDocument
            {
                EventId = envelope.EventId,
                AggregateId = envelope.AggregateId,
                EventType = envelope.EventType,
                Payload = JsonSerializer.Serialize(envelope.Payload),
                Timestamp = envelope.TimeStamp
            };
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
        }

        public async Task<IEnumerable<T>> GetEventsByAggregateIdAsync<T>(Guid aggregateId, CancellationToken ct)
        {
            var filter = Builders<EventDocument>.Filter.Eq(e => e.AggregateId, aggregateId);

            var docs = await _collection.Find(filter).ToListAsync(ct);

            return docs.Select(d => d.ToEvent<T>());
        }

        public async Task<IEnumerable<T>> GetAllEventsAsync<T>(CancellationToken ct)
        {
            var docs = await _collection.Find(_ => true).ToListAsync(ct);
            return docs.Select(d => d.ToEvent<T>());
        }
    }
}
