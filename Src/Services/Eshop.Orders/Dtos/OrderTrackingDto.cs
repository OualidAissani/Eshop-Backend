using Eshop.Orders.Data.Enums;

namespace Eshop.Orders.Dtos
{
    public class OrderTrackingDto
    {
        public string CustomerName { get; set; }
        public string Phone { get; set; }

        public string OrderItems { get; set; }
        public string ShippingAddress { get; set; }
        public string Wilaya { get; set; }
        public string Commune { get; set; }
        public OrderStatus Status { get; set; }
    }
}
