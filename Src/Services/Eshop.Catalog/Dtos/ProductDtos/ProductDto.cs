using Eshop.Catalog.Data.Enums;
using Eshop.Catalog.Dtos.CategoryDtos;
using Eshop.Catalog.Entities;
using MediaDtoType = Eshop.Catalog.Dtos.MediaDto.MediaDto;

namespace Eshop.Catalog.Dtos.ProductDtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; }
        public List<MediaDtoType> Media { get; set; }
        public List<CategoryDto> Categories { get; set; }
        public bool IsHeroFeatured { get; set; }
        public int? HeroOrder { get; set; }
        public string? HeroImageUrl { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
    }
}
