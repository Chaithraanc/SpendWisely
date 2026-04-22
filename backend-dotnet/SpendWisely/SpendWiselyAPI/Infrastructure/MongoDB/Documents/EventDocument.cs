using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.MongoDB.Documents
{
    public class EventDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; } // Changed from object to string
        public DateTime Timestamp { get; set; }

        public static EventDocument FromEvent<T>(T @event)
        {
            return new EventDocument
            {
                Id = ObjectId.GenerateNewId(),
                EventId = ExtractEventId(@event),
                AggregateId = ExtractAggregateId(@event),
                EventType = ExtractEventType(@event),
                Payload = JsonSerializer.Serialize(@event),
                Timestamp = DateTime.UtcNow
            };
        }

        public T ToEvent<T>()
        {
            return JsonSerializer.Deserialize<T>(Payload); // Payload is now string
        }

        private static Guid ExtractEventId<T>(T @event)
        {
            var prop = typeof(T).GetProperty("EventId");
            return prop != null ? (Guid)prop.GetValue(@event) : Guid.NewGuid();
        }

        private static string ExtractEventType<T>(T @event)
        {
            var prop = typeof(T).GetProperty("EventType");
            return prop != null ? (string)prop.GetValue(@event) : typeof(T).FullName;
        }

        private static Guid ExtractAggregateId<T>(T @event)
        {
            var prop = typeof(T).GetProperty("AggregateId") ??
                       typeof(T).GetProperty("ExpenseId") ??
                       typeof(T).GetProperty("EntityId") ??
                       typeof(T).GetProperty("BudgetId") ??
                       typeof(T).GetProperty("Id");

            return prop != null ? (Guid)prop.GetValue(@event) : Guid.Empty;
        }
    }
}
