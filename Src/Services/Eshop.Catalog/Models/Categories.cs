using System.ComponentModel.DataAnnotations;

namespace Eshop.Catalog.Models
{
    public class Categories
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Products> Products { get; set; }
    }
}
