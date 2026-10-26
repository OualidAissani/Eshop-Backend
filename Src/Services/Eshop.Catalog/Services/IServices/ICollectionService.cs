using Eshop.Catalog.Dtos.CollectionDtos;
using FluentResults;

namespace Eshop.Catalog.Services.IServices
{
    public interface ICollectionService
    {
        Task<Result<AddCollectionDto>> AddCollection(AddCollectionDto collection, CancellationToken ct);
        Task<List<CollectionDto>> GetCollections(CancellationToken ct);
        Task<CollectionDto> GetCollection(int id, CancellationToken ct);
        Task UpdateCollection(int id, UpdateCollectionDto collection, CancellationToken ct);
        Task DeleteCollection(int id, CancellationToken ct);
    }
}
