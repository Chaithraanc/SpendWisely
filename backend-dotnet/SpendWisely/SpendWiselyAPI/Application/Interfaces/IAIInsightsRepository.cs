using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IAIInsightsRepository
    {
        Task<AIInsights?> GetAIInsightsAsync(Guid userId, int year, int month);
        Task InsertAIInsightsAsync(AIInsights insights);
        Task UpdateAIInsightsAsync(AIInsights insights);
        Task<bool> CheckExistsAIInsightsAsync(Guid userId, int year, int month);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
