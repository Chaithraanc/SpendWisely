using SpendWiselyAPI.Application.DTOs.Expenses;
using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Mapping
{
    public static class ExpenseMappingExtensions
    {
        // Map Domain Expense to ExpenseResponse DTO
        public static ExpenseResponse ToResponse(this Expense expense)
        {
            return new ExpenseResponse
            {
                Id = expense.Id,
                UserId = expense.UserId,
                Amount = expense.Amount,
                Description = expense.Description,
                CategoryId = expense.CategoryId,
                CreatedAt = expense.CreatedAt
            };
        }
    }
}
