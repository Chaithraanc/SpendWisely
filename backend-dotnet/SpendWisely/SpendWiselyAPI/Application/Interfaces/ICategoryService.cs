using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
   
        public interface ICategoryService
        {
            Task<List<Category>> GetAllCategoriesAsync(Guid? userId);
            Task<Category> GetCategoryByIdAsync(Guid id);

            Task<Category> CreateCategoryAsync(string name, Guid? userId);
            Task UpdateCategoryAsync(Guid id, string name);
            Task DeleteCategoryAsync(Guid id);
        }
    
}
