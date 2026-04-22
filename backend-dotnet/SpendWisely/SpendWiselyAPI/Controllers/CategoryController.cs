using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.DTOs.Category;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAllCategories(Guid userId)
        {
            var categories = await _service.GetAllCategoriesAsync(userId);
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
        {
            var category = await _service.CreateCategoryAsync(request.Name, request.UserId);
            return Ok(category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryRequest request)
        {
            await _service.UpdateCategoryAsync(id, request.Name);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            await _service.DeleteCategoryAsync(id);
            return NoContent();
        }
    }
}
