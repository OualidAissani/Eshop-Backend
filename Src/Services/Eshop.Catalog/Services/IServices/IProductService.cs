using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;

namespace Eshop.Catalog.Services.IServices
{
    public interface  IProductService
    {
        Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId);

        Task<dynamic> CreateProduct(ProductCreateDto product);

        Task<Products> UpdateProduct(Products product, Stream mediafile, string contentType, string filename);

        Task<bool> DeleteProduct(int productId);

        Task<Products> GetProductById(int productId);

        Task<PaginatedResult<Products>> GetProductsAsync(PaginationParams paging);

        Task<List<Products>> GetAllProducts();

        Task<List<Products>> GetProductsByCategory(int categoryId);

        Task<bool> AssignProductToCategory(int productId, int categoryId, CancellationToken ct);

    }
}
