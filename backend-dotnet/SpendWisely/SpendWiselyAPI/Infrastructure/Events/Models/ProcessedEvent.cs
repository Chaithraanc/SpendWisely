namespace SpendWiselyAPI.Infrastructure.Events.Models
{
    public class ProcessedEvent
    {
        public Guid EventId { get; set; }
        public Guid? AggregateId { get; set; }
        public string EventType { get; set; }
        public Guid? UserId { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
