namespace SpendWiselyAPI.Application.Events
{
    public class MonthlySummaryGeneratedEvent
    {
        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }   //DashboardMonthlySummary.Id    

        public string EventType { get; set; } = null!; // "MonthlySummaryGenerated"
        public Guid UserId { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

    
        public DateTime Timestamp { get; set; }
        
    }
}
