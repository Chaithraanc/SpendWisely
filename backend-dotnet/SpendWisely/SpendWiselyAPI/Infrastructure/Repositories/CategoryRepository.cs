using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task AddCategoryAsync(Category category)
        {
            var entity = CategoryMapper.ToEntity(category);
            _context.Categories.AddAsync(entity);
           return Task.CompletedTask;
        }

        public async Task<Category> GetCategoryByIdAsync(Guid id)
        {
            var entity = await _context.Categories.FindAsync(id);
            return entity != null ? CategoryMapper.ToDomain(entity) : null;
        }

        public async Task<List<Category>> GetCategoriesByUserAsync(Guid userId)
        {
            var entities = await _context.Categories
                                         .Where(c => c.UserId == userId)
                                         .ToListAsync();

            return entities.Select(CategoryMapper.ToDomain).ToList();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var entities = await _context.Categories
                                         .Where(c => c.UserId == null)
                                         .ToListAsync();

            return entities.Select(CategoryMapper.ToDomain).ToList();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            var entity = await _context.Categories.FindAsync(category.Id);

            if (entity != null)
            {
                entity.Name = category.Name;
                entity.UpdatedAt = DateTime.UtcNow;

                
            }
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var entity = await _context.Categories.FindAsync(id);

            if (entity != null)
            {
                _context.Categories.Remove(entity);
                
            }
        }

        public async Task<bool> CheckCategoryExistsAsync(string name, Guid? userId)
        {
            return await _context.Categories
                .AnyAsync(c => c.Name == name && c.UserId == userId);
        }
    }
}
