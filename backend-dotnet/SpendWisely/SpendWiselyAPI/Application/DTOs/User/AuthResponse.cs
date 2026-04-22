namespace SpendWiselyAPI.Application.DTOs.User
{
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
