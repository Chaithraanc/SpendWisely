namespace SpendWiselyAPI.Infrastructure.Events.Models
{
    public class OutboxFailedEvent
    {
        public Guid Id { get; set; }  // primary key
        public string EventType { get; set; } = null!; // e.g., "ExpenseCreated"
        public Guid AggregateId { get; set; } // ExpenseId
        public string Payload { get; set; } = null!; // JSON string of the event

        public int RetryCount { get; set; } = 0; // number of retry attempts
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
        public string FailureReason { get; set; } = null!; // error message for debugging
    }
}
