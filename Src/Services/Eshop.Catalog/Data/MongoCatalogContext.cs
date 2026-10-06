using Eshop.Catalog.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Eshop.Catalog.Data
{
    public class MongoCatalogContext : IDisposable
    {
        private readonly IMongoClient _client;

        public MongoCatalogContext(IMongoClient client, IOptions<MongoSettings> settings)
        {
            _client = client;
            Console.WriteLine($"Database = '{settings.Value.Database}'");
            Console.WriteLine($"ProductsCollection = '{settings.Value.ProductsCollection}'");

            var database = client.GetDatabase(settings.Value.Database);
            Products = database.GetCollection<ProductDocument>(settings.Value.ProductsCollection);
            Categories = database.GetCollection<CategoryDocument>(settings.Value.CategoriesCollection);
            Counters = database.GetCollection<CounterDocument>(settings.Value.CountersCollection);
            Discounts = database.GetCollection<DiscountDocument>(settings.Value.DiscountsCollection);


        }
        public async Task EnsureIndexesAsync()
        {
            //await Discounts.Indexes.CreateOneAsync(new CreateIndexModel<DiscountDocument>(
            //    Builders<DiscountDocument>.IndexKeys.Ascending(d => d.ProductId),
            //    new CreateIndexOptions { Unique = true }));

            await Products.Indexes.CreateOneAsync(new CreateIndexModel<ProductDocument>(
                Builders<ProductDocument>.IndexKeys.Ascending(p => p.ProductId)));

            await Products.Indexes.CreateOneAsync(new CreateIndexModel<ProductDocument>(
                Builders<ProductDocument>.IndexKeys.Text(p => p.Title).Text(p => p.Description)));
        }

        public IMongoCollection<ProductDocument> Products { get; }
        public IMongoCollection<DiscountDocument> Discounts { get; }
        public IMongoCollection<CategoryDocument> Categories { get; }
        public IMongoCollection<CounterDocument> Counters { get; }

        public void Dispose()
        {
            if (_client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
