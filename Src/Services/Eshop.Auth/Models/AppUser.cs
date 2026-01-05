using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eshop.Auth.Models
{
    public class AppUser:IdentityUser<Guid>
    {
        [NotMapped]
        public string[] Role { get; set; }
    }
}
