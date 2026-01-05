using System.ComponentModel.DataAnnotations;

namespace Eshop.Orders.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int TotalPrice { get; set; }
        public DateTime OrderCreationDate { get; set; } = DateTime.UtcNow;
        public List<OrderItem> OrderItems { get; set; } 
        public string UserId { get; set; }
    }
}
