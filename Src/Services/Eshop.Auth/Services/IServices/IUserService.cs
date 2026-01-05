using Eshop.Auth.Models;

namespace Eshop.Auth.Services.IServices
{
    public interface IUserService
    {
        Task<AppUser> GetUserByIdAsync(Guid userId);
        Task<AppUser> GetUserByRefreshTokenAsync(string refreshToken);
    }
}
