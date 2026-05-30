using Eshop.Catalog.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Eshop.Catalog.Data
{
    public class MongoCatalogContext
    {
        public MongoCatalogContext(IMongoClient client, IOptions<MongoSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.Database);
            Products = database.GetCollection<ProductDocument>(settings.Value.ProductsCollection);
            Categories = database.GetCollection<CategoryDocument>(settings.Value.CategoriesCollection);
            Counters = database.GetCollection<CounterDocument>(settings.Value.CountersCollection);
        }

        public IMongoCollection<ProductDocument> Products { get; }
        public IMongoCollection<CategoryDocument> Categories { get; }
        public IMongoCollection<CounterDocument> Counters { get; }
    }
}
