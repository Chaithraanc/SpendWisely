using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IBudgetService
    {
        Task<Budget?> GetBudgetByIdAsync(Guid id);

        Task<IEnumerable<Budget>> GetBudgetsByUserAsync(Guid userId);

        Task<Budget?> GetBudgetByUserCategoryMonthYearAsync(
            Guid userId,
            Guid? categoryId,
            int month,
            int year);

        Task<Budget> CreateBudgetAsync(Budget budget, CancellationToken ct);

        Task<Budget> UpdateBudgetAsync(Budget budget, CancellationToken ct);

        Task DeleteBudgetAsync(Guid budgetId, CancellationToken ct);

        Task<bool> CheckBudgetExistsAsync(Guid userId, Guid? categoryId, int month, int year);

    }
}
