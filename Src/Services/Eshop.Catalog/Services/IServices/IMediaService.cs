using Eshop.Catalog.Dtos;
using Eshop.Catalog.Entities;

namespace Eshop.Catalog.Services.IServices
{
    public interface IMediaService
    {
        Task<ProductMediaItem> CreateMedia(ProductMediaItem media, Stream fileStream, string contentType, string fileName, CancellationToken ct);

        Task<bool> DeleteMedia(string uuid,CancellationToken ct);

        Task<List<ProductMediaItem>> ProductMedias(ProductCreateDto product, List<IFormFile> formFile, CancellationToken ct);

        Task DeleteOldProductMedia(ProductDocument product, CancellationToken ct);
    }
}
