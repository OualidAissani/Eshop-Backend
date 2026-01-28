using System.ComponentModel.DataAnnotations;

namespace Eshop.Inventory.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int ProductId { get; set; }
    }
}
