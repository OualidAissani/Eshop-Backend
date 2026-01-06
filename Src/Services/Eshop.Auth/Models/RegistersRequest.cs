using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Eshop.Auth.Models
{
    public class RegistersRequest
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        [PasswordPropertyText]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
