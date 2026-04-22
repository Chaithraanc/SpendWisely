namespace SpendWiselyAPI.Application.Events
{
    public class BudgetEvent
    {
        public Guid EventId { get; set; }

        public string EventType { get; set; } = null!; // e.g., "BudgetCreated"
        public Guid BudgetId { get; set; }

        public Guid UserId { get; set; }

        public Guid? CategoryId { get; set; }

        public decimal Amount { get; set; }

        public int Month { get; set; }  
        public int Year { get;  set; }
        public DateTime Timestamp { get; set; }
    }
}
