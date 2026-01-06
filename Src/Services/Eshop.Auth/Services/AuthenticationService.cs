using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;
using MassTransit.NewIdProviders;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Claims;

namespace Eshop.Auth.Services
{
    public class AuthenticationService:IAuthenticationService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtTokenService _Jwtservice;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserService _userService;

        public AuthenticationService(UserManager<AppUser> userManager,IJwtTokenService jwtTokenService,
            IConfiguration configuration ,IRefreshTokenRepository refreshTokenRepository,IUserService userService)
        {
            _userManager = userManager;
            _Jwtservice = jwtTokenService;
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
            _userService = userService;
        }
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var refreshToken = string.Empty;
            //checking the email exisitence
           var user= await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return null;
            }
            // checking the crendial 
            var check=await _userManager.CheckPasswordAsync(user,request.Password);
            if (!check)
            {
                return null;
            }
            // retriveing user claims
             var userclaim=  await _userManager.GetClaimsAsync(user);
            var role = userclaim.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).First();

            //generating an access Token
            var accessToken= _Jwtservice.GenerateAccessToken(user.Id.ToString(), user.Email, user.UserName,new string[] {role});

            //checking validity of the refresh token 
             var checkingrefreshTokenExistence = await _refreshTokenRepository.GetRefreshTokenByUserId(user.Id);
            if (checkingrefreshTokenExistence!=null && (checkingrefreshTokenExistence.Token != null || checkingrefreshTokenExistence.ExpiryDate > DateTime.UtcNow))
            {
                refreshToken = checkingrefreshTokenExistence.Token;
            }
            else
            {
                //regenerating a new refresh token if the condition not met
                 refreshToken = await _Jwtservice.GenerateRefreshToken();
                var refreshTokenResult = await _refreshTokenRepository.CreateSaveRefreshToken(new RefreshToken()
                {
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshTokenExpiryDays")),
                    UserId = user.Id,

                });

                if (refreshTokenResult <= 0)
                {
                    return null;
                }
            }

            var userDto = new UserDto()
            {
                Email = user.Email,
                UserName = user.UserName,
                Id = user.Id,
                Roles = user.Role
            };
            // response 
            var loginResponse=new LoginResponse
            {
                accessToken = accessToken,
                refreshToken = refreshToken,
                User = userDto
            };

            return loginResponse;
        }
        public async Task<LoginResponse> RegisterAsync(RegistersRequest request)
        {
            var check = await _userManager.FindByEmailAsync(request.Email);
            if (check!=null)
            {
                return null;
            }
            if(request.UserName.Contains(' '))
            {
                return null;
            }
            var user = new AppUser()
            {
                Email=request.Email,
                UserName=request.UserName,
                NormalizedUserName=request.UserName.ToUpper()
            };
            var newuserresult=await _userManager.CreateAsync(user, request.Password);
            if (!newuserresult.Succeeded)
            {
                foreach(var err in newuserresult.Errors)
                {
                    Console.WriteLine($"User Submission Issue:{err.Description}");

                }

                return null;
            }
           var roleresult= await _userManager.AddClaimAsync(user,new Claim(ClaimTypes.Role, Roles.Client));
            if (!roleresult.Succeeded)
            {
                Console.WriteLine($"Role Issue: {roleresult.Errors}");
                return null;
            }
            user.Role= new string[] { Roles.Client };
            var accessToken= _Jwtservice.GenerateAccessToken(user.Id.ToString(),user.Email,user.UserName, user.Role);
            var refreshToken=await _Jwtservice.GenerateRefreshToken();
            var refreshTokenResult = await _refreshTokenRepository.CreateSaveRefreshToken(new RefreshToken()
            {
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshTokenExpiryDays")),
                UserId = user.Id,

            });

            if (refreshTokenResult <= 0)
            {
                Console.WriteLine("Refresh Token");

                return null;
            }
            var userDto = new UserDto()
            {
                Email = user.Email,
                UserName = user.UserName,
                Id = user.Id,
                Roles = user.Role
            };
            var loginResponse = new LoginResponse
            {
                accessToken = accessToken,
                refreshToken = refreshToken,
                User = userDto
            };
            return loginResponse;
        }
        public async Task<LoginResponse> RefreshTokenAsync(string RefreshToken)
        {
            //retrive user of the refresh token
            var user = await _userService.GetUserByRefreshTokenAsync(RefreshToken);
            if (user == null)
            {
                return null;
            }
            var userclaim = await _userManager.GetClaimsAsync(user);
            var role = userclaim.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).First();
            //refereshing the token
            var refreshedToken = await _Jwtservice.GenerateRefreshToken();
            
            if(await _refreshTokenRepository.SaveNewRefreshedToken(RefreshToken, refreshedToken) <= 0)
            {
                return null;
            }
            //generating a new access token
            var accessToken = _Jwtservice.GenerateAccessToken(user.Id.ToString(), user.Email, user.UserName, new string[] { role });
            if (accessToken !=null) {
                var userDto = new UserDto()
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    Id = user.Id,
                    Roles = user.Role
                };
                return new LoginResponse
                {
                    accessToken = accessToken,
                    refreshToken = refreshedToken,
                    User = userDto
                };
            }
            return null;
        
            
        }
    }
}
