using BCrypt.Net;
using SpendWiselyAPI.Application.DTOs.User;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Domain.Entities;
using SpendWiselyAPI.Infrastructure.DbContext;

namespace SpendWiselyAPI.Application.Services
{
    public class UserService :IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwt;
        private readonly AppDbContext _dbContext;

        public UserService(IUserRepository userRepository, IJwtTokenGenerator jwt, AppDbContext dbContext)
        {
            _userRepository = userRepository;
            _jwt = jwt;
            _dbContext = dbContext;
        }

        public async Task<User> RegisterUserAsync(string name, string email, string password, string role = "User")
        {
            var exists = await _userRepository.CheckUserExistsByEmailAsync(email);
            if (exists)
                throw new Exception("User already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User(name, email, passwordHash, role);

            await _userRepository.AddUserAsync(user);
            _dbContext.SaveChanges();
            return user;
        }

        public async Task<AuthResponse> LoginUserAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                throw new Exception("Invalid email or password");

            var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isValid)
                throw new Exception("Invalid email or password");

            var accessToken = _jwt.GenerateToken(user);
            var refreshToken = Infrastructure.Authentication.RefreshTokenGenerator.Generate();
            var refreshExpiry = DateTime.UtcNow.AddDays(7);

            user.SetRefreshToken(refreshToken, refreshExpiry);
            await _userRepository.UpdateUserAsync(user);
            _dbContext.SaveChanges();
            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null ||
                user.RefreshToken != refreshToken ||
                user.RefreshTokenExpiry == null ||
                user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                throw new Exception("Invalid refresh token");
            }

            var newAccessToken = _jwt.GenerateToken(user);
            var newRefreshToken = Infrastructure.Authentication.RefreshTokenGenerator.Generate();
            var newExpiry = DateTime.UtcNow.AddDays(7);

            user.SetRefreshToken(newRefreshToken, newExpiry);
            await _userRepository.UpdateUserAsync(user);
            _dbContext.SaveChanges();

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }
    }
}
