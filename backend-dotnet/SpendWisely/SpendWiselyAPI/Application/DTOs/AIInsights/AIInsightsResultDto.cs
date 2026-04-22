namespace SpendWiselyAPI.Application.DTOs.AIInsights
{
    public class AIInsightsResultDto
    {
        public Guid UserId { get; init; }
        public string Summary { get; set; }
        public List<string> Recommendations { get; set; }
        public List<SpendingSpikeDto> SpendingSpikes { get; set; }
        public List<AnomalyDto> Anomalies { get; set; }
        public ForecastDto Forecast { get; set; }
    }

    public class SpendingSpikeDto
    {
        public Guid CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class AnomalyDto
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class ForecastDto
    {
        public ForecastDetailDto NextMonth { get; set; }
        public ForecastDetailDto YearEnd { get; set; }
    }

    public class ForecastDetailDto
    {
        public decimal PredictedSpending { get; set; }
        public double Confidence { get; set; }
    }

}
