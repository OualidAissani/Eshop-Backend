using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eshop.Catalog.Models
{
    public class CategoryDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
