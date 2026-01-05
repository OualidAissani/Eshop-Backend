using System.Security.Claims;

namespace Eshop.Auth.Services.IServices
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(string userId, string email, string name, string[] roles);
        Task<string> GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
    }
}
