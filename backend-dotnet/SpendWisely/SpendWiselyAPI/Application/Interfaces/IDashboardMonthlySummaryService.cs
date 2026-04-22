using SpendWiselyAPI.Application.DTOs.DashboardMonthlySummary;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IDashboardMonthlySummaryService
    {
        Task<MonthlySummaryResponse?> GetMonthlySummaryAsync(Guid userId, int year, int month);
        Task<List<MonthlySummaryResponse>> GetYearlySummaryAsync(Guid userId, int year);
    }
}
