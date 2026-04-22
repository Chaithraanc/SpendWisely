using SpendWiselyAPI.Application.DTOs.User;
using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
    
        public interface IUserService
        {
        Task<User> RegisterUserAsync(string name, string email, string password, string role = "User");
        Task<AuthResponse> LoginUserAsync(string email, string password);
        Task<AuthResponse> RefreshTokenAsync(Guid userId, string refreshToken);
        Task<User> GetUserByIdAsync(Guid id);
    }
    
}
