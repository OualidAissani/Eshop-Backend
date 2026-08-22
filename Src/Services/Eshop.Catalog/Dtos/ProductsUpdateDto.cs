using Eshop.Catalog.Data.Enums;
using Eshop.Catalog.Entities;

namespace Eshop.Catalog.Dtos
{
    public class ProductsUpdateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; }
        public List<int>? CategoriesId { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    }
}
