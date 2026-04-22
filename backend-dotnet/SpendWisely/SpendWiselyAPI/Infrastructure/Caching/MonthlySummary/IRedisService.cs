namespace SpendWiselyAPI.Infrastructure.Caching.MonthlySummary
{
    public interface IRedisService
    {
        Task SetUserTotalAsync(Guid userId, decimal total);
        Task SetUserCategoryTotalAsync(Guid userId, Guid categoryId, decimal total);

        Task<decimal?> GetUserTotalAsync(Guid userId);
        Task<decimal?> GetUserCategoryTotalAsync(Guid userId, Guid categoryId);

        Task ResetUserAsync(Guid userId);
        Task ResetAllUsersAsync();

        Task SetMonthlySummaryGeneratedAsync(int year, int month);
        Task<bool> HasMonthlySummaryGeneratedAsync(int year, int month);

        Task SetMonthlyAIInsightsGeneratedAsync(int year, int month);
        Task<bool> HasMonthlyAIInsightsGeneratedAsync(int year, int month);

    }
}
