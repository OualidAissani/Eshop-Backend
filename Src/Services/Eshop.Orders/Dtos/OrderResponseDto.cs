using Eshop.Orders.Data.Enums;
using Eshop.Orders.Entities;

namespace Eshop.Orders.Dtos
{
    public class OrderResponseDto
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public decimal TotalPrice { get; set; }

        public string Email { get; set; }

        public OrderStatus Status { get; set; }

        public string ShippingAddress { get; set; }

        public string Phone { get; set; }

        public string Wilaya { get; set; }

        public string Commune { get; set; }

        public PaymentMethods PayementMethod { get; set; }

        public DateTime OrderedAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public List<OrderItemResponseDto> OrderItems { get; set; }

        public string UserId { get; set; }
    }
}
