
using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Application.Services
{
    public class DashboardMonthlySummaryService : IDashboardMonthlySummaryService
    {
        private readonly IDashboardMonthlySummaryRepository _repo;

        public DashboardMonthlySummaryService(IDashboardMonthlySummaryRepository repo)
        {
            _repo = repo;
        }

        public async Task<MonthlySummaryResponse?> GetMonthlySummaryAsync(Guid userId, int year, int month)
        {
            // Fetch total row (CategoryId = null)
            var totalRow = await _repo.GetTotalRowAsync(userId, year, month);
            if (totalRow == null)
                return null;

            // Fetch category rows
            var categories = await _repo.GetMonthlyBreakdownAsync(userId, year, month);

            return new MonthlySummaryResponse
            {
                Year = year,
                Month = month,
                TotalSpent = totalRow.TotalSpent,
                Categories = categories
                    .Where(c => c.CategoryId != null)
                    .Select(c => new MonthlySummaryCategoryDto
                    {
                        CategoryId = c.CategoryId!.Value,
                        TotalSpent = c.TotalSpent
                    })
                    .ToList()
            };
        }

        public async Task<List<MonthlySummaryResponse>> GetYearlySummaryAsync(Guid userId, int year)
        {
            var rows = await _repo.GetYearlyAsync(userId, year);

            return rows
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g =>
                {
                    var total = g.FirstOrDefault(x => x.CategoryId == null);

                    return new MonthlySummaryResponse
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalSpent = total?.TotalSpent ?? 0,
                        Categories = g
                            .Where(x => x.CategoryId != null)
                            .Select(x => new MonthlySummaryCategoryDto
                            {
                                CategoryId = x.CategoryId!.Value,
                                TotalSpent = x.TotalSpent
                            })
                            .ToList()
                    };
                })
                .OrderBy(x => x.Month)
                .ToList();
        }
    }
}
