using Eshop.Auth.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace Eshop.Auth.Services.IServices
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegistersRequest request);
        Task<LoginResponse> RefreshTokenAsync(string RefreshToken);
    }
}
