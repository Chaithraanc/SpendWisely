using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIInsightsController : ControllerBase
    {
        private readonly IAIInsightsService _insightsService;

        public AIInsightsController(IAIInsightsService insightsService)
        {
            _insightsService = insightsService;
        }

        [HttpGet("{userId}/{year:int}/{month:int}")]
        public async Task<IActionResult> GetInsights(Guid userId, int year, int month)
        {
            var result = await _insightsService.GetAIInsightsAsync(userId, year, month);

            if (result == null)
                return NotFound(new { message = "No insights available for this period." });

            return Ok(result);
        }
    }
}
