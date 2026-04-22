using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.DTOs.AIInsights;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.DbContext;
using System.Text.Json;

namespace SpendWiselyAPI.Application.Services
{
    public class AIInsightsService : IAIInsightsService
    {
        private readonly AppDbContext _context;

        public AIInsightsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AIInsightsResultDto?> GetAIInsightsAsync(Guid userId, int year, int month)
        {
            var entity = await _context.AIInsights
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Year == year && x.Month == month);

            if (entity == null || string.IsNullOrWhiteSpace(entity.Insights))
                return null;

            var dto = JsonSerializer.Deserialize<AIInsightsResultDto>(entity.Insights,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return dto;
        }
    }
}
