using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
    public static class DashboardMonthlySummaryMapper
    {
        // Domain → EF Core entity
        public static DashboardMonthlySummaryEntity ToEntity(this DashboardMonthlySummary domain)
        {
            return new DashboardMonthlySummaryEntity
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Month = domain.Month,
                Year = domain.Year,
                CategoryId = domain.CategoryId,
                TotalSpent = domain.TotalSpent,
                CreatedAt = domain.CreatedAt,
                UpdatedAt = domain.UpdatedAt
            };
        }

        // EF Core entity → Domain
        public static DashboardMonthlySummary ToDomain(this DashboardMonthlySummaryEntity entity)
        {
            return new DashboardMonthlySummary
            (
                entity.Id,
                entity.UserId,
                entity.Month,
                entity.Year,
                entity.CategoryId,
                entity.TotalSpent,
                entity.CreatedAt,
                entity.UpdatedAt
            );
        }

        // domain → API DTO
        public static MonthlySummaryResponse ToDto(this List<DashboardMonthlySummary> rows)
        {
            var totalRow = rows.First(r => r.CategoryId == null);

            return new MonthlySummaryResponse
            {
                UserId = totalRow.UserId,
                Month = totalRow.Month,
                Year = totalRow.Year,
                TotalSpent = totalRow.TotalSpent,

                Categories = rows
                    .Where(r => r.CategoryId != null)
                    .Select(r => new MonthlySummaryCategoryDto
                    {
                        CategoryId = r.CategoryId!.Value,
                        TotalSpent = r.TotalSpent
                    })
                    .ToList()
            };
        }
    }
}
