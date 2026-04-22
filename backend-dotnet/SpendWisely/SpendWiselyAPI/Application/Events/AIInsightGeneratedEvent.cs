namespace SpendWiselyAPI.Application.Events
{
    public class AIInsightGeneratedEvent
    {
        public Guid EventId { get; set; } 
        public Guid AggregateId { get; set; } // AIInsights.Id

        public string EventType { get; set; } = null!; // "AIInsightGenerated"
        public Guid UserId { get; set; }

        public int Month { get;  set; }
        public int Year { get; set; }

        public string Insights { get; set; } // JSON from OpenAI
        public DateTime Timestamp { get; set; }
   
    }
}
