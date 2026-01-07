namespace Eshop.Orders.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double FullPrice { get; set; }
        public int InventoryId { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
    }
}
