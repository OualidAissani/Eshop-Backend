using Eshop.Catalog.Data.Enums;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eshop.Catalog.Entities
{
    public class ProductDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        
        public string Id { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
        public ProductSpecialStatus SpecialStatus { get; set; }
        public int? DisplayOrder { get; set; }
        public List<ProductMediaItem> Media { get; set; } = new();
        public List<CategoryItem> Categories { get; set; } = new();

        public bool IsHeroFeatured { get; set; }
        public int? HeroOrder { get; set; }
        public string? HeroImageUrl { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class ProductMediaItem
    {
        public string Media { get; set; }
        public string Description { get; set; }
    }

    public class CategoryItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
