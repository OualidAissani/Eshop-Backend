using System.ComponentModel;

namespace Eshop.Auth.Models
{
    public class RegistersRequest
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        [PasswordPropertyText]
        public string Password { get; set; }
    }
}
