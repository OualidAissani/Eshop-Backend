using System.Text.Json.Serialization;

namespace Eshop.Orders.Dtos
{
    public class OrderItemResponseDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal FullPrice { get; set; }

        public int InventoryId { get; set; }

        public int OrderId { get; set; }
    }
}
