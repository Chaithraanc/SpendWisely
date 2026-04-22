using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
  
        public interface ICategoryRepository
        {
            Task<Category> GetCategoryByIdAsync(Guid id);
            Task<List<Category>> GetCategoriesByUserAsync(Guid userId);   // user-specific categories
            Task<List<Category>> GetAllCategoriesAsync();                 // global categories

            Task AddCategoryAsync(Category category);
            Task UpdateCategoryAsync(Category category);
            Task DeleteCategoryAsync(Guid id);

            Task<bool> CheckCategoryExistsAsync(string name, Guid? userId);
        }
    
}
