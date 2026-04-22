using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(Guid id);
        Task<User> GetUserByEmailAsync(string email);
        Task<List<User>> GetAllUsersAsync();

        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(Guid id);

        Task<bool> CheckUserExistsByEmailAsync(string email);
    }
}
