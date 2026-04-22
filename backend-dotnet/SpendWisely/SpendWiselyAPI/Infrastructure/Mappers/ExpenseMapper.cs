using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
    // Infrastructure/Mappers/ExpenseMapper.cs
    // This class provides mapping methods to convert between the Expense domain entity
    // and the ExpenseEntity database model used by Entity Framework Core.
    public static class ExpenseMapper
        {
            public static Expense ToDomain(ExpenseEntity entity)
            {
            // Use the hydration constructor so Id/CreatedAt/UpdatedAt are preserved
            return new Expense(
                entity.Id,
                entity.UserId,
                entity.Amount,
                entity.Description,
                entity.CategoryId,
                entity.CreatedAt,
                entity.UpdatedAt
            );
        }

            public static ExpenseEntity ToEntity(Expense expense)
            {
                return new ExpenseEntity
                {
                    Id = expense.Id,
                    UserId = expense.UserId,
                    Amount = expense.Amount,
                    Description = expense.Description,
                    CategoryId = expense.CategoryId,
                    CreatedAt = expense.CreatedAt,
                    UpdatedAt = expense.UpdatedAt
                };
            }
        }
    
}
