using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using FluentResults;

namespace Eshop.Catalog.Services.IServices
{
    public interface  IProductService
    {
        Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId, CancellationToken ct);

        Task<Result<ProductDto>> CreateProduct(ProductCreateDto product, List<IFormFile>? formFile, CancellationToken ct);
        Task<Result<ProductDto>> UpdateProduct(int ProductId, ProductsUpdateDto productDto, List<IFormFile>? formFile,CancellationToken ct, bool ImageAppend = false);

        Task<Result<bool>> DeleteProduct(int productId,CancellationToken ct);
        Task<Result<ProductDto>> DeleteProductReturnOldProduct(int productId, CancellationToken ct);
        Task<ProductDto> GetProductById(int productId, CancellationToken ct);

        Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging, CancellationToken ct);

        Task<List<ProductDto>> GetAllProducts(CancellationToken ct);

        Task<List<ProductDto>> GetProductsByCategory(int categoryId, CancellationToken ct);

        Task<Result<bool>> AssignProductToCategory(int productId, int categoryId, CancellationToken ct);

        Task<List<ProductDto>> ProductSearch(string tag, CancellationToken ct);

        Task<Result<ProductDto>> UpdateHeroSelection(int productId, ProductHeroUpdateDto dto, CancellationToken ct);
        Task<List<ProductDto>> GetHeroProducts(CancellationToken ct);


    }
}
