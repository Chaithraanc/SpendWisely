using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetExpenseByIdAsync(Guid id);

        Task<List<Expense>> GetExpensesByUserAsync(Guid userId);

        Task AddExpenseAsync(Expense expense);

        Task UpdateExpenseAsync(Expense expense);

        Task DeleteExpenseAsync(Guid id);
        Task UpdateExpenseCategoryAsync(Guid expenseId, string category);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<List<UserMonthlyCategoryTotal>> GetAggregatedTotalsForMonthAsync(int year, int month);
    }
}
