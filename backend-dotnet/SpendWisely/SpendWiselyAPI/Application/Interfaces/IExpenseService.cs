using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
   
        public interface IExpenseService
        {
            Task<Expense> GetExpenseByIdAsync(Guid id);
            Task<List<Expense>> GetExpensesByUserAsync(Guid userId);

            Task<Expense> CreateExpenseAsync(Guid userId, decimal amount, string description, Guid? categoryId);
            Task<Expense> UpdateExpenseAsync(Guid expenseId, decimal amount, string description, Guid? categoryId);
            Task DeleteExpenseAsync(Guid expenseId);
        }
    
}
