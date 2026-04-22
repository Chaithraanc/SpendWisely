namespace SpendWiselyAPI.Domain.Entities
{
   
        public class AIInsights
        {
            public Guid Id { get; private set; }
            public Guid UserId { get; private set; }
            public int Month { get; private set; }
            public int Year { get; private set; }
            public string Insights { get; private set; } // JSON from OpenAI
            public DateTime CreatedAt { get; private set; }
            public DateTime? UpdatedAt { get; private set; }

            private AIInsights() { } // EF Core

            public AIInsights(Guid userId, int year, int month, string insightsJson)
            {
                Id = Guid.NewGuid();
                UserId = userId;
                Year = year;
                Month = month;
                Insights = insightsJson;
                CreatedAt = DateTime.UtcNow;
            }

        //Hydrate existing insights 
        public AIInsights(Guid id, Guid userId, int year, int month, string insightsJson, DateTime createdAt, DateTime? updatedAt)
        {
            Id = id;
            UserId = userId;
            Year = year;
            Month = month;
            Insights = insightsJson;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public void UpdateInsights(string insightsJson)
            {
                Insights = insightsJson;
                UpdatedAt = DateTime.UtcNow;
            }
        }
}
