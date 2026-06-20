namespace Eshop.Catalog.Dtos
{
    public class ProductHeroUpdateDto
    {
        public bool IsHeroFeatured { get; set; }
        public int? HeroOrder { get; set; }
        public string? HeroImageUrl { get; set; }
    }
}
