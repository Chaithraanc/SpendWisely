using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
    public class CategoryMapper
    {
        // Domain → Entity
        public static CategoryEntity ToEntity(Category category)
        {
            return new CategoryEntity
            {
                Id = category.Id,
                Name = category.Name,
                UserId = category.UserId,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        // Entity → Domain
        public static Category ToDomain(CategoryEntity entity)
        {
            return new Category
            (
                entity.Id,
                entity.Name,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.UserId
            ); 
            
        }
    }
}
