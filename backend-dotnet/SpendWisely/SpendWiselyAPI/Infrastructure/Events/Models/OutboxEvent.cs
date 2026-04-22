namespace SpendWiselyAPI.Infrastructure.Events.Models
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }  // primary key

        public string EventType { get; set; } = null!; // e.g., "ExpenseCreated"

        public Guid AggregateId { get; set; } // ExpenseId

        public string Payload { get; set; } = null!; // JSON string of the event

        public bool Processed { get; set; } = false; // published or not

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int RetryCount { get; set; } = 0; // for retry logic

        public DateTime? PublishedAtUTC { get; set; } // when it was published

        public DateTime? ScheduledAtUTC { get; set; } // when it should be published (for delayed retries)

        public string? LastErrorMessage { get; set; } = null!; // store last error message for debugging
    }
}
