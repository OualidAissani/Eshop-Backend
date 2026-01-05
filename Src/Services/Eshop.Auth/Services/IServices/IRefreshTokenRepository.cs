using Eshop.Auth.Models;

namespace Eshop.Auth.Services.IServices
{
    public interface IRefreshTokenRepository
    {
        Task<int> CreateSaveRefreshToken(RefreshToken refreshToken);
    }
}
