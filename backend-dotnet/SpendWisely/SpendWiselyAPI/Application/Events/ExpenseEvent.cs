namespace SpendWiselyAPI.Application.Events
{
    public class ExpenseEvent
    {
        public Guid EventId { get; set; }

        public string EventType { get; set; } = null!; // e.g., "ExpenseCreated"
        public Guid ExpenseId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
