namespace Eshop.Web.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public double Rating { get; set; }
  public int ReviewCount { get; set; }
    public bool InStock { get; set; } = true;
}
