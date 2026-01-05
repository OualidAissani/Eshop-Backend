namespace Eshop.Auth.Models
{
    public class LoginResponse
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public UserDto User { get; set; }
    }
}
