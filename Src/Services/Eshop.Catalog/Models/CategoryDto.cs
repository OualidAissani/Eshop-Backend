namespace Eshop.Catalog.Models
{
    public class CategoryDto
    {
        //    public record CategoryDto(Guid Id, string Name, string Slug, bool IsActive, Guid? ParentId, int SortOrder);//add them as attributes
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }

    }
}
