using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;
using MassTransit.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eshop.Auth.Services
{
    public class JwtTokenService: IJwtTokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _algorithm;
        private readonly int _accessTokenExpiryMinutes;
        public JwtTokenService(string secretKey,string issuer,string audience,string algorithm,int accessTokenExpiryMinutes) {
        
            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _algorithm = algorithm;
            _accessTokenExpiryMinutes = accessTokenExpiryMinutes;

        }
        public string GenerateAccessToken(string userId, string email, string name, string[] roles)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Name, name),
                new("userId", userId)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                signingCredentials: new SigningCredentials(key, _algorithm)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using(var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                
            }

            return Convert.ToBase64String(randomNumber);
        }
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenvalidation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            }; 
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenvalidation, out SecurityToken validatedToken);
            return principal;
        }


    }
}
