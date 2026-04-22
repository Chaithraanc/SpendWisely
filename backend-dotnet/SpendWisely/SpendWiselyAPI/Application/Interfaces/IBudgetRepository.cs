using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IBudgetRepository
    {
        Task<Budget?> GetBudgetByIdAsync(Guid id);

        Task<Budget?> GetBudgetByUserCategoryMonthYearAsync(
            Guid userId,
            Guid? categoryId,
            int month,
            int year);

        Task<IEnumerable<Budget>> GetBudgetsByUserAsync(Guid userId);
        Task<IEnumerable<Budget>> GetBudgetsByUserYearAsync(Guid userId, int year);
        Task<IEnumerable<Budget>> GetBudgetsByYearAsync(int year);



        Task AddBudgetAsync(Budget budget);

        Task UpdateBudgetAsync(Budget budget);

        Task DeleteBudgetAsync(Budget budget);

        Task<bool> CheckBudgetExistsAsync(Guid userId, Guid? categoryId, int month, int year);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }

}
