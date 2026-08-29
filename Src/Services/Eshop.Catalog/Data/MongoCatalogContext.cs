using Eshop.Catalog.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Eshop.Catalog.Data
{
    public class MongoCatalogContext
    {
        public MongoCatalogContext(IMongoClient client, IOptions<MongoSettings> settings)
        {
            Console.WriteLine($"Database = '{settings.Value.Database}'");
            Console.WriteLine($"ProductsCollection = '{settings.Value.ProductsCollection}'");

            var database = client.GetDatabase(settings.Value.Database);
            Products = database.GetCollection<ProductDocument>(settings.Value.ProductsCollection);
            Categories = database.GetCollection<CategoryDocument>(settings.Value.CategoriesCollection);
            Counters = database.GetCollection<CounterDocument>(settings.Value.CountersCollection);
            Discounts = database.GetCollection<DiscountDocument>(settings.Value.DiscountsCollection);
        }

        public IMongoCollection<ProductDocument> Products { get; }
        public IMongoCollection<DiscountDocument> Discounts { get; }
        public IMongoCollection<CategoryDocument> Categories { get; }
        public IMongoCollection<CounterDocument> Counters { get; }
    }
}
