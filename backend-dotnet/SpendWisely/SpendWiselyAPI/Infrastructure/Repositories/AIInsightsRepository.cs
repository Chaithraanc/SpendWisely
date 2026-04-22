using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class AIInsightsRepository : IAIInsightsRepository
    {
        private readonly AppDbContext _db;

        public AIInsightsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AIInsights?> GetAIInsightsAsync(Guid userId, int year, int month)
        {
            var entity = await _db.AIInsights
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Year == year &&
                    x.Month == month);

            return entity?.ToDomain();
        }

        public async Task<bool> CheckExistsAIInsightsAsync(Guid userId, int year, int month)
        {
            return await _db.AIInsights
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.Year == year &&
                    x.Month == month);
        }

        public async Task InsertAIInsightsAsync(AIInsights insights)
        {
            await _db.AIInsights.AddAsync(insights.ToEntity());
        }

        public Task UpdateAIInsightsAsync(AIInsights insights)
        {
            _db.AIInsights.Update(insights.ToEntity());
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
