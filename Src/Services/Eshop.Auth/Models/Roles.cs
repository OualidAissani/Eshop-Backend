using Microsoft.AspNetCore.Identity;

namespace Eshop.Auth.Models
{
    public class Roles:IdentityRole<Guid>
    {
        static public string Admin = "Admin";
        static public string Client = "Client";
    }
}
