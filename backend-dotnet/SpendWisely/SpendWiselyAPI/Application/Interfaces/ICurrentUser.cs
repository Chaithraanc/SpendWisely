namespace SpendWiselyAPI.Application.Interfaces
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
        string Email { get; }
        string Role { get; }
    }
}
