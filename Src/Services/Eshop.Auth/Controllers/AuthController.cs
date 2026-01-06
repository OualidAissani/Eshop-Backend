using Eshop.Auth.Models;
using Eshop.Auth.Services.IServices;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        public AuthController(IAuthenticationService authentication)
        {
            _authService = authentication;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) 
        {
            var logins=await _authService.LoginAsync(request);
            if(logins==null)
            {
                return NotFound();
            }
            return Ok(logins);

        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistersRequest request)
        {
            var logins = await _authService.RegisterAsync(request);
            if (logins == null)
            {
                
                return BadRequest();
            }
            return Ok(logins);

        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            return Ok(await _authService.RefreshTokenAsync(refreshToken));
        }
    }
}
