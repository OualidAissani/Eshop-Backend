namespace Eshop.Orders.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int FullPrice { get; set; }
        public int InventoryId { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
    }
}
