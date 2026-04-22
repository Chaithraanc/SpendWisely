using SpendWiselyAPI.Application.DTOs.AIInsights;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IAIInsightsService
    {
        Task<AIInsightsResultDto?> GetAIInsightsAsync(Guid userId, int year, int month);
    }
}
