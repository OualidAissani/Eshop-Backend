namespace Eshop.Auth.Models
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string[]? Roles { get; set; }
    }
}
