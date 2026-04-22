namespace SpendWiselyAPI.Application.Events
{
    public class ExpenseCategorizedEvent
    {
        public Guid EventId { get; set; }
        public Guid ExpenseId { get; set; }

        public string EventType { get; set; } = null!;
        public Guid UserId { get; set; }
        
        public string Category { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
