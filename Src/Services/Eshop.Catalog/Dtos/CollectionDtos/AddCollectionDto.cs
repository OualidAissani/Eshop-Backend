namespace Eshop.Catalog.Dtos.CollectionDtos
{
    public class AddCollectionDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public List<int> ProductId { get; set; }
    }
}
