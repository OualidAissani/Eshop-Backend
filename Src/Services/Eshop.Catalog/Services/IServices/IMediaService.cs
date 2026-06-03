using Eshop.Catalog.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Catalog.Services.IServices
{
    public interface IMediaService
    {
        Task<ProductMediaItem> CreateMedia(ProductMediaItem media, Stream fileStream, string contentType, string fileName, CancellationToken ct);

        Task<bool> DeleteMedia(string uuid,CancellationToken ct);
    }
}
