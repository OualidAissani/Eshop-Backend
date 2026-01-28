using System.ComponentModel.DataAnnotations;

namespace Eshop.Catalog.Models
{
    public class ProductMedia
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Media { get; set; }
        public string Description { get; set; }
        public int ProductId { get; set; }
        public Products Product { get; set; }
    }
}
