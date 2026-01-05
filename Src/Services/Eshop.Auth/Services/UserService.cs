using Eshop.Auth.Data;
using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Auth.Services
{
    public class UserService:IUserService
    {
        private readonly AuthDbContext _context;
        public UserService(AuthDbContext context)
        {
            _context = context;
        }
        public async Task<AppUser> GetUserByIdAsync(Guid userId)
        {
            var user= await _context.Users.FindAsync(userId);
            return user;
        }
        public async Task<AppUser> GetUserByRefreshTokenAsync(string refreshToken)
        {
            var refreshTokenob=await _context.RefreshTokens.Where(I=>I.Token==refreshToken &&I.ExpiryDate< DateTime.UtcNow).FirstOrDefaultAsync();
            if(refreshTokenob==null)
            {
                return null;
            }
            return await _context.Users.FindAsync(refreshTokenob.UserId);
            
        }
    }
}
