using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;

namespace Eshop.Catalog.Services.IServices
{
    public interface  IProductService
    {
        Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId);

        Task<dynamic> CreateProduct(ProductCreateDto product,CancellationToken ct);

        Task<ProductDto> UpdateProduct(Products product, Stream mediafile, string contentType, string filename, CancellationToken ct);

        Task<bool> DeleteProduct(int productId,CancellationToken ct);

        Task<ProductDto> GetProductById(int productId);

        Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging);

        Task<List<ProductDto>> GetAllProducts();

        Task<List<ProductDto>> GetProductsByCategory(int categoryId);

        Task<bool> AssignProductToCategory(int productId, int categoryId, CancellationToken ct);

        Task<List<ProductDto>> ProductSearch(string tag);


    }
}
