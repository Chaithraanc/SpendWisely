using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.Events.Models
{
    public class DomainEventEnvelope<TEvent>
    {
        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }
        public Guid UserId { get; set; }
        public string EventType { get; set; }   // <— KEY FIELD
        public TEvent Payload { get; set; } // <— RAW JSON
        public DateTime TimeStamp { get; set; }
    }
}
