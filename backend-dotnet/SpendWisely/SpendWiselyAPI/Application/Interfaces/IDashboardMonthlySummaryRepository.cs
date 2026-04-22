using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;
using SpendWiselyAPI.Workers.DashboardSummaryGenerator;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IDashboardMonthlySummaryRepository
    {
        Task<DashboardMonthlySummary?> GetAllDashboardMonthlySummaryAsync(Guid userId, int year, int month, Guid? categoryId);
        Task<List<DashboardMonthlySummary>> GetMonthlyBreakdownAsync(Guid userId, int year, int month);// This will return all rows for the month, including the total row (with NULL categoryId)
        Task<DashboardMonthlySummary?> GetTotalRowAsync(Guid userId, int year, int month);// Total row is the one with NULL categoryId
        Task<List<DashboardMonthlySummary>> GetYearlyAsync(Guid userId, int year);// This will return all rows for the year, including the total row (with NULL categoryId)

        Task AddDashboardMonthlySummaryAsync(DashboardMonthlySummary entity);
        Task UpdateDashboardMonthlySummaryAsync(DashboardMonthlySummary entity);

        Task<List<UserMonthlyCategoryTotal>> GetMonthlySummaryAsync(int year, int month);
        Task<List<UserMonthlyCategoryTotal>> GetYearlySummaryAsync(int year);
        Task UpsertMonthlySummaryAsync(int year, int month, IReadOnlyCollection<UserMonthlyCategoryTotal> totals);


    }
}
