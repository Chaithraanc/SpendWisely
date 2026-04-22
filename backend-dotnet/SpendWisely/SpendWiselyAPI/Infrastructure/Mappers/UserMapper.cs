using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Mappers
{
     public static class UserMapper
        {
            // Domain → Entity
            public static UserEntity ToEntity(User user)
            {
            return new UserEntity
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            }

            // Entity → Domain
            public static User ToDomain(UserEntity entity)
            {
                var user = new User(
                    entity.Id,
                    entity.Name,
                    entity.Email,
                    entity.PasswordHash,
                    entity.Role,
                    entity.CreatedAt,
                    entity.UpdatedAt

                );

              

                return user;
            }
        }
}
