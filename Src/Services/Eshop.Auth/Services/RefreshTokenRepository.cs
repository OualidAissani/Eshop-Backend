using Eshop.Auth.Data;
using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;

namespace Eshop.Auth.Services
{
    public class RefreshTokenRepository:IRefreshTokenRepository
    {
        private readonly AuthDbContext _context;
        public RefreshTokenRepository(AuthDbContext context)
        {
            _context = context;
        }
        public async Task<int> CreateSaveRefreshToken(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            return await _context.SaveChangesAsync();
        } 
    }
}
