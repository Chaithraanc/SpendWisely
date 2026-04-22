using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;
using SpendWiselyAPI.Infrastructure.Models;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;
using System.Collections.Generic;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class DashboardMonthlySummaryRepository : IDashboardMonthlySummaryRepository
    {
        private readonly AppDbContext _db;

        public DashboardMonthlySummaryRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardMonthlySummary?> GetAllDashboardMonthlySummaryAsync(Guid userId, int year, int month, Guid? categoryId)
        {
            var entity = await _db.DashboardMonthlySummary
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Year == year &&
                    x.Month == month &&
                    x.CategoryId == categoryId);
            return entity?.ToDomain();
        }

        public async Task<List<DashboardMonthlySummary>> GetMonthlyBreakdownAsync(Guid userId, int year, int month)
        {
            var entities = await _db.DashboardMonthlySummary
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.Year == year &&
                    x.Month == month &&
                    x.CategoryId != null)
                .OrderBy(x => x.CategoryId)
                .ToListAsync();
            return entities.Select(e => e.ToDomain()).ToList();
        }   
                 
        

        public async Task<DashboardMonthlySummary?> GetTotalRowAsync(Guid userId, int year, int month)
        {
            var entity =  await _db.DashboardMonthlySummary
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Year == year &&
                    x.Month == month &&
                    x.CategoryId == null);
            return entity?.ToDomain();
        }

        public async Task<List<DashboardMonthlySummary>> GetYearlyAsync(Guid userId, int year)
        {
            var entities = await _db.DashboardMonthlySummary
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Year == year)
                .OrderBy(x => x.Month)
                .ToListAsync();
            return entities.Select(e => e.ToDomain()).ToList();
        }

        public async Task AddDashboardMonthlySummaryAsync(DashboardMonthlySummary entity)
        {
            await _db.DashboardMonthlySummary.AddAsync(entity.ToEntity());
           
        }

        public async Task UpdateDashboardMonthlySummaryAsync(DashboardMonthlySummary entity)
        {
            _db.DashboardMonthlySummary.Update(entity.ToEntity());
          
        }

        public async Task UpsertMonthlySummaryAsync(
            int year,
            int month,
          IReadOnlyCollection<UserMonthlyCategoryTotal> totals)
          {
            if (totals == null || totals.Count == 0)
                return;

            var userIds = totals.Select(t => t.UserId).Distinct().ToList();
            var dashboardlist = GetMonthlyBreakdownAsync(userIds.First(), year, month).Result;
            var existing = await _db.DashboardMonthlySummary
                .Where(x => x.Year == year
                            && x.Month == month
                            && userIds.Any(id => id == x.UserId)

                            //&& userIds.Contains(x.UserId)
                            )
                .ToListAsync();
            Dictionary<(Guid UserId, Guid? CategoryId), DashboardMonthlySummaryEntity> existingLookup;
            if (existing.Count == 0)
            {
                existingLookup = new Dictionary<(Guid UserId, Guid? CategoryId), DashboardMonthlySummaryEntity>();
            }
            else
            {
                existingLookup = existing
                    .ToDictionary(
                        x => (x.UserId, x.CategoryId),
                        x => x);
            }
            foreach (var t in totals)
            {
                var key = (t.UserId, t.CategoryId);

                if (existingLookup.TryGetValue(key, out var entity))
                {
                    // update
                    entity.UpdateEntityTotal(t.TotalSpent);
                }
                else
                {
                    // insert
                    var newEntity = new DashboardMonthlySummary(
                        id: Guid.NewGuid(),
                        userId: t.UserId,
                        month: month,
                        year: year,
                        categoryId: t.CategoryId,
                        totalSpent: t.TotalSpent);

                    await _db.DashboardMonthlySummary.AddAsync(newEntity.ToEntity());
                }
            }

         
        }


        public async Task<List<UserMonthlyCategoryTotal>> GetMonthlySummaryAsync(int year, int month)
        {
            var query =
                from s in _db.DashboardMonthlySummary
                where s.Year == year && s.Month == month
                select new UserMonthlyCategoryTotal
                {
                    UserId = s.UserId,
                    CategoryId = s.CategoryId,
                    TotalSpent = s.TotalSpent
                };

          
            return await query.ToListAsync();
        }

        public async Task<List<UserMonthlyCategoryTotal>> GetYearlySummaryAsync(int year)
        {
            var query =
                from s in _db.DashboardMonthlySummary
                where s.Year == year
                select new UserMonthlyCategoryTotal
                {
                    UserId = s.UserId,
                    CategoryId = s.CategoryId,
                    TotalSpent = s.TotalSpent
                };


            return await query.ToListAsync();
        }
    }
}
