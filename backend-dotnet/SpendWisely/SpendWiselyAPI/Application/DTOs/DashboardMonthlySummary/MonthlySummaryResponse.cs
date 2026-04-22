namespace SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary
{
    public class MonthlySummaryResponse
    {
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalSpent { get; set; }

        public List<MonthlySummaryCategoryDto> Categories { get; set; } = new();
    }
}
