namespace Eshop.Web.Models;

public class Category
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
public string IconUrl { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
