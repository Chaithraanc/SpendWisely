using System.Security.Cryptography;

namespace SpendWiselyAPI.Infrastructure.Authentication
{
    public static class RefreshTokenGenerator
    {
        public static string Generate()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
