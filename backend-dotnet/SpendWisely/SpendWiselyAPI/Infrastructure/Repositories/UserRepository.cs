using Microsoft.EntityFrameworkCore;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;
using SpendWiselyAPI.Infrastructure.Mappers;
using SpendWiselyAPI.Infrastructure.Models;

namespace SpendWiselyAPI.Infrastructure.Repositories
{
    public class UserRepository :IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task AddUserAsync(User user)
        {
            var entity = UserMapper.ToEntity(user);
             _context.Users.AddAsync(entity);
            return Task.CompletedTask;

        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            var entity = await _context.Users.FindAsync(id);
            return entity != null ? UserMapper.ToDomain(entity) : null;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var entity = await _context.Users
                                       .FirstOrDefaultAsync(u => u.Email == email);

            return entity != null ? UserMapper.ToDomain(entity) : null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var entities = await _context.Users.ToListAsync();
            return entities.Select(UserMapper.ToDomain).ToList();
        }

        public async Task UpdateUserAsync(User user)
        {
            var entity = await _context.Users.FindAsync(user.Id);

            if (entity != null)
            {
                entity.Name = user.Name;
                entity.Email = user.Email;
                entity.PasswordHash = user.PasswordHash;
                entity.Role = user.Role;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.RefreshToken = user.RefreshToken;
                entity.RefreshTokenExpiry = user.RefreshTokenExpiry;
                


            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var entity = await _context.Users.FindAsync(id);

            if (entity != null)
            {
                _context.Users.Remove(entity);
                
            }
        }

        public async Task<bool> CheckUserExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
