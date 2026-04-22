using SpendWiselyAPI.Application.DTOs.Budget;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
    public static class BudgetMapper
    {
        public static Budget ToDomain(BudgetEntity entity)
        {
            // Map the BudgetEntity to a Budget domain entity-use hydration constructor to peserve values of private setters
            return new Budget
            
                (
                entity.Id,
                entity.UserId,
                entity.CategoryId,
                entity.Amount,
                entity.Month,
                entity.Year,
                entity.CreatedAt,
                entity.UpdatedAt
            );
        }
        public static BudgetEntity ToEntity(Budget budget)
        {
            return new BudgetEntity
            {
                Id = budget.Id,
                UserId = budget.UserId,
                CategoryId = budget.CategoryId,
                Amount = budget.Amount,
                Month = budget.Month,
                Year = budget.Year,
                CreatedAt = budget.CreatedAt,
                UpdatedAt = budget.UpdatedAt
            };
        }

        public static BudgetResponse ToDto(this Budget budget)
        {
            return new BudgetResponse
            {
                Id = budget.Id,
                UserId = budget.UserId,
                CategoryId = budget.CategoryId,
                Amount = budget.Amount,
                Month = budget.Month,
                Year = budget.Year,
                CreatedAt = budget.CreatedAt,
                UpdatedAt = budget.UpdatedAt
            };
        }

    }
}
