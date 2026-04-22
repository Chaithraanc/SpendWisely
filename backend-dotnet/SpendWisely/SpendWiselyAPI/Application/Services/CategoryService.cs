using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;

namespace SpendWiselyAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly AppDbContext _dbContext;

        public CategoryService(ICategoryRepository categoryRepository , AppDbContext dbContext)
        {
            _categoryRepository = categoryRepository;
            _dbContext = dbContext;
        }

        public async Task<Category> CreateCategoryAsync(string name, Guid? userId)
        {
            // Prevent duplicate category per user
            var exists = await _categoryRepository.CheckCategoryExistsAsync(name, userId);
            if (exists)
                throw new Exception("Category already exists");

            var category = new Category(name, userId);

            await _categoryRepository.AddCategoryAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        public async Task<List<Category>> GetAllCategoriesAsync(Guid? userId)
        {
            var global = await _categoryRepository.GetAllCategoriesAsync();
            var userCategories = new List<Category>();
            if (userId != null)
                userCategories = await _categoryRepository.GetCategoriesByUserAsync(userId.Value);

            global.AddRange(userCategories);
            return global;
        }

        public async Task<Category> GetCategoryByIdAsync(Guid id)
        {
            return await _categoryRepository.GetCategoryByIdAsync(id);
        }

        public async Task UpdateCategoryAsync(Guid id, string name)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);

            if (category == null)
                throw new Exception("Category not found");

            category.UpdateName(name);

            await _categoryRepository.UpdateCategoryAsync(category);
                await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            await _categoryRepository.DeleteCategoryAsync(id);
                await _dbContext.SaveChangesAsync();
        }
    }
}
