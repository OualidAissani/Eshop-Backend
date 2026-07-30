using System.ComponentModel.DataAnnotations;

namespace Eshop.Orders.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public List<CartItem> CartItems { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }

        public string UserId { get; set; }


    } 

}
