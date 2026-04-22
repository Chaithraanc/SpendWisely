
using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardMonthlySummaryService _service;
        private readonly ICurrentUser _currentUser;

        public DashboardController(IDashboardMonthlySummaryService service ,ICurrentUser currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        [HttpGet("{year:int}/{month:int}")]
        public async Task<IActionResult> GetMonthly(Guid userId, int year, int month)
        {
            var result = await _service.GetMonthlySummaryAsync(userId, year, month);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("{year:int}")]
        public async Task<IActionResult> GetYearly(Guid userId, int year)
        {
            var result = await _service.GetYearlySummaryAsync(userId, year);
            return Ok(result);
        }
    }
}
