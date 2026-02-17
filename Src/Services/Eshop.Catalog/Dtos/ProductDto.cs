using Eshop.Catalog.Models;

namespace Eshop.Catalog.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; }
        public List<MediaDto> Media { get; set; }
        public List<CategoryDto> Categories { get; set; }
    }
}
