using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.DTOs.Budget;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly IBudgetService _budgetService;

        public BudgetController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        // GET: api/budget/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBudgetById(Guid id)
        {
            var budget = await _budgetService.GetBudgetByIdAsync(id);
            if (budget == null)
                return NotFound();

            return Ok(BudgetResponse.FromDomain(budget));
        }

        // GET: api/budget/user/{userId}
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetBudgetsByUser(Guid userId)
        {
            var budgets = await _budgetService.GetBudgetsByUserAsync(userId);
            return Ok(budgets.Select(BudgetResponse.FromDomain));
        }

        // POST: api/budget
        [HttpPost]
        public async Task<IActionResult> CreateBudget(
            [FromBody] CreateBudgetRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var budget = request.ToDomain();

            try
            {
                var created = await _budgetService.CreateBudgetAsync(budget, ct);
                return CreatedAtAction(nameof(GetBudgetById),
                    new { id = created.Id },
                    BudgetResponse.FromDomain(created));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/budget/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateBudget(
            Guid id,
            [FromBody] UpdateBudgetRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var budget = request.ToDomain(id);

            try
            {
                var updated = await _budgetService.UpdateBudgetAsync(budget, ct);
                return Ok(BudgetResponse.FromDomain(updated));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE: api/budget/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteBudget(Guid id, CancellationToken ct)
        {
            try
            {
                await _budgetService.DeleteBudgetAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
