using Eshop.Auth.Data;
using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;
using Microsoft.EntityFrameworkCore;

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
        public async Task<int> SaveNewRefreshedToken(string refreshToken,string refreshedToken)
        {
            var CurrentRf =await _context.RefreshTokens.Where(t => t.Token == refreshToken).FirstAsync();
            CurrentRf.Token = refreshedToken;
            CurrentRf.ExpiryDate = DateTime.UtcNow.AddDays(7);
             _context.RefreshTokens.Update(CurrentRf);
            return await _context.SaveChangesAsync();

        }
        public async Task<RefreshToken> GetRefreshTokenByUserId(Guid UserId)
        {
            return await _context.RefreshTokens.Where(i => i.UserId.Equals(UserId)).AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
