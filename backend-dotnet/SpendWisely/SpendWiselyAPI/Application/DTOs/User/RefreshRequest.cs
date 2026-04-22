namespace SpendWiselyAPI.Application.DTOs.User
{
    public class RefreshRequest
    {
        public Guid UserId { get; set; }
        public string RefreshToken { get; set; } = default!;
    }
}
