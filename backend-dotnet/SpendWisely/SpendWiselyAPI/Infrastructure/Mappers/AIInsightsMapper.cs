using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
    public static class AIInsightsMapper
    {
        // Domain → Entity
        public static AIInsightsEntity ToEntity(this AIInsights domain)
        {
            return new AIInsightsEntity
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Month = domain.Month,
                Year = domain.Year,
                Insights = domain.Insights,
                CreatedAt = domain.CreatedAt,
                UpdatedAt = domain.UpdatedAt
            };
        }

        // Entity → Domain

        public static AIInsights ToDomain(this AIInsightsEntity entity)
        {
            return new AIInsights(
                id: entity.Id,
                userId: entity.UserId,
                year: entity.Year,
                month: entity.Month,
                insightsJson: entity.Insights,
                createdAt: entity.CreatedAt,
                updatedAt: entity.UpdatedAt);
        }
    }
}
