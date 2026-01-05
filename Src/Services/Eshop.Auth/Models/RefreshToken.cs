namespace Eshop.Auth.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public Guid UserId { get; set; }
        public AppUser User { get; set; }
    }
}
