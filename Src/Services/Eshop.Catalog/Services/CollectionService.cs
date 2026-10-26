using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos.CollectionDtos;
using Eshop.Catalog.Entities;
using Eshop.Catalog.Services.IServices;
using FluentResults;
using MongoDB.Driver;

namespace Eshop.Catalog.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly MongoCatalogContext _context;
        public CollectionService(MongoCatalogContext context)
        {
            _context = context;
        }
        public async Task<Result<AddCollectionDto>> AddCollection(AddCollectionDto collection, CancellationToken ct)
        {
            if (collection is null)
                return Result.Fail("Collection cannot be null.");

            if(collection.Name is null || collection.Name.Length < 3)
                return Result.Fail("Collection name must be at least 3 characters long.");

            if(collection.Description is null || collection.Description.Length < 10)
                return Result.Fail("Collection description must be at least 10 characters long.");

            var existingCollection = await _context.Collections.Find(c => c.Name == collection.Name).FirstOrDefaultAsync(ct);

            if(existingCollection != null)
                return Result.Fail($"A collection with the name '{collection.Name}' already exists.");

            var collectionMapping =new CollectionDocument
            {
                Name = collection.Name,
                Description = collection.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            try
            {
                await _context.Collections.InsertOneAsync(collectionMapping, cancellationToken: ct);

            }
            catch (Exception ex)
            {
                return Result.Fail($"An error occurred while adding the collection: {ex.Message}");
            }
            return Result.Ok(collection);
        }

        public Task DeleteCollection(int id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<CollectionDto> GetCollection(int id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<List<CollectionDto>> GetCollections(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCollection(int id, UpdateCollectionDto collection, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
