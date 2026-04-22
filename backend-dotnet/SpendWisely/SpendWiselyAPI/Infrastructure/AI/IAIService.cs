using SpendWiselyAPI.Application.DTOs.AIInsights;

namespace SpendWiselyAPI.Infrastructure.AI
{
    public interface IAIService
    {
        Task<string> CategorizeExpenseAsync(string description);
        Task<string> GenerateMonthlyInsightsBatchAsync(
            AIInsightsBatchInput batch,
                    CancellationToken cancellationToken = default);
    }
}
