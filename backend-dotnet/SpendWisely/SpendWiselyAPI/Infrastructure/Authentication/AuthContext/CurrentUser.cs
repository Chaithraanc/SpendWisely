using SpendWiselyAPI.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SpendWiselyAPI.Infrastructure.Authentication.AuthContext
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _context;

        public CurrentUser(IHttpContextAccessor context)
        {
            _context = context;
        }

        public Guid UserId
        {
            get
            {
                var user = _context.HttpContext?.User;
                var id = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                return id != null ? Guid.Parse(id) : Guid.Empty;
            }
        }

        public string Email =>
            _context.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

        public string Role =>
            _context.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}

