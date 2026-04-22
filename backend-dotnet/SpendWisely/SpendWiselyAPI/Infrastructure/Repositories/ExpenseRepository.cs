using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly AppDbContext _context;

        public ExpenseRepository(AppDbContext context)
        {
            _context = context;
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            return _context.Database.BeginTransactionAsync(ct);
        }
        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _context.SaveChangesAsync(ct);
        }
        public Task AddExpenseAsync(Expense expense)
        {
            var entity = ExpenseMapper.ToEntity(expense);
            _context.Expenses.AddAsync(entity);
            return Task.CompletedTask;


        }

        public async Task DeleteExpenseAsync(Guid id)
        {
            var entity = await _context.Expenses.FindAsync(id);
            if (entity != null)
            {
                _context.Expenses.Remove(entity);

            }
        }

        public async Task<Expense?> GetExpenseByIdAsync(Guid id)
        {
            var entity = await _context.Expenses
                                       .Include(e => e.Category)
                                       .FirstOrDefaultAsync(e => e.Id == id);
            return entity != null ? ExpenseMapper.ToDomain(entity) : null;
        }

        public async Task<List<Expense>> GetExpensesByUserAsync(Guid userId)
        {
            var entities = await _context.Expenses
                                         .Where(e => e.UserId == userId)
                                         .Include(e => e.Category)
                                         .ToListAsync();
            return entities.Select(ExpenseMapper.ToDomain).ToList();
        }

        public async Task UpdateExpenseAsync(Expense expense)
        {
            var entity = await _context.Expenses.FindAsync(expense.Id);
            if (entity != null)
            {
                entity.Amount = expense.Amount;
                entity.Description = expense.Description;
                entity.CategoryId = expense.CategoryId;
                entity.UpdatedAt = System.DateTime.UtcNow;


            }
        }

        public async Task UpdateExpenseCategoryAsync(Guid expenseId, string category)
        {
            var entity = await _context.Expenses.FindAsync(expenseId);
            if (entity != null)
            {
                // Find category
                var catEntity = await _context.Categories.FirstOrDefaultAsync(c => c.Name == category);

                entity.CategoryId = catEntity.Id;
                entity.UpdatedAt = System.DateTime.UtcNow;
            }

        }

        public async Task<List<UserMonthlyCategoryTotal>> GetAggregatedTotalsForMonthAsync(int year, int month)
        {
            return await _context.Database
                .SqlQuery<UserMonthlyCategoryTotal>(
                    $"EXEC sp_GetAggregatedTotalsForMonth {year}, {month}")
                .ToListAsync();
        }
    }
}

