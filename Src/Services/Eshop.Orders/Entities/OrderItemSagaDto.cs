using Eshop.Events;

namespace Eshop.Orders.Models
{
    public class OrderItemSagaDto
    {
        public string name { get; set; }

        public string quantity { get; set; }

        public string description { get; set; }

        public AmountDto unit_amount { get; set; }
    }
}
