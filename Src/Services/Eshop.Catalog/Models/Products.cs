using System.ComponentModel.DataAnnotations;

namespace Eshop.Catalog.Models
{
    public class Products
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; }
        public List<ProductMedia> Media { get; set; }
        public List<Categories> Categories { get; set; }
    }
}
