using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eshop.Catalog.Models
{
    public class CounterDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
