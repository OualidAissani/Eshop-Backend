using Eshop.Orders.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Eshop.Orders.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "Ord-" + Guid.NewGuid().ToString()+" -"+ DateTime.UtcNow.Hour;

        public decimal TotalPrice { get; set; }
        public string Email { get; set; }

        public OrderStatus Status { get; set; }

        public string ShippingAddress { get; set; }

        public string Phone { get; set; }

        public string Wilaya { get; set; }

        public string Commune { get; set; }

        public PaymentMethods PayementMethod { get; set; }

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public List<OrderItem> OrderItems { get; set; } 

        public string UserId { get; set; }
    }
}
