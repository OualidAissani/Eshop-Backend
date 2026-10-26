using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eshop.Catalog.Entities
{
    public class CollectionDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public List<int> ProductIds { get; set; } 
    }
}
