using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.DTOs.Expenses;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Application.Mapping; // for ToResponse()

namespace SpendWiselyAPI.Controllers
{
   // [Authorize]// All endpoints require authentication
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
        {
            var expense = await _expenseService.CreateExpenseAsync(
                request.UserId,
                request.Amount,
                request.Description,
                request.CategoryId
            );

            return Ok(expense.ToResponse());
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetExpenseByUser(Guid userId)
        {
            var expenses = await _expenseService.GetExpensesByUserAsync(userId);

            return Ok(expenses.Select(e => e.ToResponse()));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseRequest request)
        {
            var updated = await _expenseService.UpdateExpenseAsync(
                expenseId: id,
                amount: request.Amount,
                description: request.Description,
                categoryId: request.CategoryId
            );

            return Ok(updated.ToResponse());
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid id)
        {
            await _expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }
    }
}
