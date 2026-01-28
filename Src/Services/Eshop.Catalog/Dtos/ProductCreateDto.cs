using Eshop.Catalog.Models;

namespace Eshop.Catalog.Dtos
{
    public class ProductCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; } = 0;
        public List<int>? Categories { get; set; }

    }
}
