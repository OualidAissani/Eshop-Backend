namespace Eshop.Catalog.Data
{
    public class MongoSettings
    {
        public string Database { get; set; }
        public string ProductsCollection { get; set; } = "products";
        public string CategoriesCollection { get; set; } = "categories";
        public string CountersCollection { get; set; } = "counters";
        public string DiscountsCollection { get; set; } = "discounts";
    }
}
