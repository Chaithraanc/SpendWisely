namespace SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary
{
    public class MonthlySummaryCategoryDto
    {
        public Guid CategoryId { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
