using SpendWiselyAPI.Application.DTOs.Budget;
using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;

namespace SpendWiselyAPI.Application.DTOs.AIInsights
{
    public class AIInsightsInput
    {
        public Guid UserId { get; init; }

        // Current month summary (the one that triggered the event)
        public IReadOnlyList<UserMonthlyCategoryTotal> CurrentMonthSummary { get; init; }

        // Historical summaries for previous months in the same year
        public IReadOnlyList<UserMonthlyCategoryTotal> HistoricalSummaries { get; init; }

        // Budget allocations for all categories for the year
        public IReadOnlyList<BudgetResponse> BudgetAllocations { get; init; }

        public int Month { get; init; }
        public int Year { get; init; }
    }
}
