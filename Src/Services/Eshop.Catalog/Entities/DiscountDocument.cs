using Eshop.Catalog.Data.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Eshop.Catalog.Entities;

public class DiscountDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int TimesUsed { get; set; }
    public int ProductId { get; set; }


    public decimal ApplyDiscount(decimal price)
    {
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
        {
            return price; // No discount applied
        }
        decimal discountedPrice = price;
        switch (Type)
        {
            case DiscountType.Percentage:
                discountedPrice -= price * (Value / 100);
                break;
            case DiscountType.FixedAmount:
                discountedPrice -= Value;
                break;
        }
        return Math.Max(discountedPrice, 0); 
    }

}



