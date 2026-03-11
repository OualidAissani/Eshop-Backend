using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using FluentResults;

namespace Eshop.Catalog.Services.IServices
{
    public interface  IProductService
    {
        Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId);

        Task<Result<ProductCreateResponseDto>> CreateProduct(ProductCreateDto product,CancellationToken ct);

        Task<Result<ProductDto>> UpdateProduct(Products product, Stream mediafile, string contentType, string filename, CancellationToken ct);

        Task<Result<bool>> DeleteProduct(int productId,CancellationToken ct);

        Task<ProductDto> GetProductById(int productId);

        Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging);

        Task<List<ProductDto>> GetAllProducts();

        Task<List<ProductDto>> GetProductsByCategory(int categoryId);

        Task<Result<bool>> AssignProductToCategory(int productId, int categoryId, CancellationToken ct);

        Task<List<ProductDto>> ProductSearch(string tag);


    }
}
