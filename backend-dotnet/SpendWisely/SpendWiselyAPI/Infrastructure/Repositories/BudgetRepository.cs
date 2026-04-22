using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class BudgetRepository : IBudgetRepository
    {
        private readonly AppDbContext _context;

        public BudgetRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Budget?> GetBudgetByIdAsync(Guid id)
        {
            var entity =  await _context.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);
            return entity != null ? BudgetMapper.ToDomain(entity) : null;
        }

        public async Task<Budget?> GetBudgetByUserCategoryMonthYearAsync(
            Guid userId,
            Guid? categoryId,
            int month,
            int year)
        {
            var entity =  await _context.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.UserId == userId &&
                    b.CategoryId == categoryId &&
                    b.Month == month &&
                    b.Year == year);
            return entity != null ? BudgetMapper.ToDomain(entity) : null;
          
        }

        public async Task<IEnumerable<Budget>> GetBudgetsByUserAsync(Guid userId)
        {
            var entities = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .ToListAsync();
            return entities.Select(BudgetMapper.ToDomain);
        }

        public async Task<IEnumerable<Budget>> GetBudgetsByUserYearAsync(Guid userId, int year)
        {
            var entities = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Year == year)
                .ToListAsync();
            return entities.Select(BudgetMapper.ToDomain);
        }

        public async Task<IEnumerable<Budget>> GetBudgetsByYearAsync(int year)
        {
            var entities = await _context.Budgets
                .AsNoTracking()
                .Where(b =>  b.Year == year)
                .ToListAsync();
            return entities.Select(BudgetMapper.ToDomain);
        }

        public Task AddBudgetAsync(Budget budget)
        {
            var entity = BudgetMapper.ToEntity(budget);
             _context.Budgets.AddAsync(entity);
            return Task.CompletedTask;
        }

        public Task UpdateBudgetAsync(Budget budget)
        {
            var entity = BudgetMapper.ToEntity(budget);
            _context.Budgets.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteBudgetAsync(Budget budget)
        {
            var entity = BudgetMapper.ToEntity(budget);
            _context.Budgets.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> CheckBudgetExistsAsync(Guid userId, Guid? categoryId, int month, int year)
        {
            return await _context.Budgets
                .AnyAsync(b =>
                    b.UserId == userId &&
                    b.CategoryId == categoryId &&
                    b.Month == month &&
                    b.Year == year);
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            return _context.Database.BeginTransactionAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _context.SaveChangesAsync(ct);
        }
    }

}
