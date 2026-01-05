using Eshop.Web.Models;

namespace Eshop.Web.Services;

public class ProductService
{
 private readonly List<Product> _products = new()
    {
        new Product
    {
       Id = 1,
            Name = "Wireless Headphones",
            Description = "Premium noise-cancelling wireless headphones with 30-hour battery life and superior sound quality.",
  Price = 299.99m,
    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&h=500&fit=crop",
         Category = "Electronics",
 IsFeatured = true,
         Rating = 4.5,
      ReviewCount = 128,
   InStock = true
    },
        new Product
        {
         Id = 2,
            Name = "Smart Watch",
            Description = "Feature-rich smartwatch with health tracking, GPS, and 5-day battery life.",
            Price = 399.99m,
            ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=500&h=500&fit=crop",
            Category = "Electronics",
            IsFeatured = true,
            Rating = 4.7,
          ReviewCount = 256,
     InStock = true
      },
        new Product
        {
         Id = 3,
    Name = "Laptop Backpack",
            Description = "Durable and stylish backpack with padded laptop compartment and water-resistant material.",
    Price = 79.99m,
            ImageUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=500&h=500&fit=crop",
    Category = "Accessories",
            IsFeatured = false,
       Rating = 4.3,
     ReviewCount = 89,
      InStock = true
        },
        new Product
        {
            Id = 4,
        Name = "Mechanical Keyboard",
          Description = "RGB backlit mechanical keyboard with customizable keys and premium switches.",
            Price = 149.99m,
            ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500&h=500&fit=crop",
 Category = "Electronics",
        IsFeatured = true,
   Rating = 4.8,
            ReviewCount = 342,
    InStock = true
        },
        new Product
    {
            Id = 5,
     Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with precision tracking and long battery life.",
            Price = 49.99m,
          ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=500&h=500&fit=crop",
 Category = "Electronics",
        IsFeatured = false,
       Rating = 4.4,
       ReviewCount = 167,
   InStock = true
  },
        new Product
        {
          Id = 6,
  Name = "USB-C Hub",
  Description = "Multi-port USB-C hub with HDMI, USB 3.0, and SD card reader.",
Price = 59.99m,
     ImageUrl = "https://images.unsplash.com/photo-1625948515291-69613efd103f?w=500&h=500&fit=crop",
  Category = "Accessories",
        IsFeatured = false,
   Rating = 4.2,
            ReviewCount = 94,
   InStock = true
        },
new Product
        {
      Id = 7,
        Name = "Desk Lamp",
            Description = "LED desk lamp with adjustable brightness and color temperature.",
         Price = 39.99m,
        ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=500&h=500&fit=crop",
      Category = "Home",
    IsFeatured = false,
         Rating = 4.6,
        ReviewCount = 78,
            InStock = true
        },
        new Product
        {
      Id = 8,
       Name = "Portable Charger",
         Description = "High-capacity portable charger with fast charging support for multiple devices.",
          Price = 34.99m,
         ImageUrl = "https://images.unsplash.com/photo-1609091839311-d5365f9ff1c5?w=500&h=500&fit=crop",
            Category = "Electronics",
            IsFeatured = true,
  Rating = 4.5,
            ReviewCount = 213,
            InStock = true
        },
        new Product
        {
       Id = 9,
     Name = "Phone Stand",
            Description = "Adjustable aluminum phone stand compatible with all smartphones and tablets.",
     Price = 24.99m,
         ImageUrl = "https://images.unsplash.com/photo-1572635196237-14b3f281503f?w=500&h=500&fit=crop",
   Category = "Accessories",
        IsFeatured = false,
          Rating = 4.1,
     ReviewCount = 45,
        InStock = true
  },
   new Product
        {
            Id = 10,
            Name = "Webcam HD",
Description = "1080p HD webcam with auto-focus and built-in noise-cancelling microphone.",
            Price = 89.99m,
            ImageUrl = "https://images.unsplash.com/photo-1588508065123-287b28e013da?w=500&h=500&fit=crop",
            Category = "Electronics",
   IsFeatured = false,
          Rating = 4.3,
            ReviewCount = 156,
    InStock = true
        },
        new Product
        {
            Id = 11,
            Name = "Monitor Stand",
 Description = "Premium wooden monitor stand with storage space for keyboard and accessories.",
 Price = 44.99m,
            ImageUrl = "https://images.unsplash.com/photo-1527689368864-3a821dbccc34?w=500&h=500&fit=crop",
        Category = "Home",
          IsFeatured = false,
            Rating = 4.7,
     ReviewCount = 67,
            InStock = true
        },
 new Product
    {
   Id = 12,
  Name = "Bluetooth Speaker",
            Description = "Waterproof Bluetooth speaker with 360-degree sound and 12-hour playtime.",
            Price = 69.99m,
            ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=500&h=500&fit=crop",
      Category = "Electronics",
IsFeatured = true,
      Rating = 4.6,
   ReviewCount = 289,
       InStock = true
        }
    };

    public Task<List<Product>> GetAllProductsAsync()
    {
        return Task.FromResult(_products);
    }

  public Task<List<Product>> GetFeaturedProductsAsync()
    {
        return Task.FromResult(_products.Where(p => p.IsFeatured).ToList());
    }

    public Task<Product?> GetProductByIdAsync(int id)
    {
        return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
    }

    public Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        return Task.FromResult(_products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList());
  }

    public Task<List<string>> GetCategoriesAsync()
    {
        return Task.FromResult(_products.Select(p => p.Category).Distinct().ToList());
    }
}
