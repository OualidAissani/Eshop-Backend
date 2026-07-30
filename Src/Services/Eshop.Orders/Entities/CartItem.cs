namespace Eshop.Orders.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal FullPrice { get; set; }

        public int CartId { get; set; }

        public Cart Cart { get; set;}

}

}
