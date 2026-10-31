using Eshop.Catalog.Data;
using Eshop.Catalog.Data.Enums;
using Eshop.Catalog.Dtos.DisocuntDtos;
using Eshop.Catalog.Entities;
using Eshop.Catalog.Services.IServices;
using FluentResults;
using MongoDB.Driver;

namespace Eshop.Catalog.Services
{
    public class DiscountService:IDiscountService
    {
        private readonly MongoCatalogContext _context;
        public DiscountService(MongoCatalogContext context)
        {
            _context = context;
        }

        public async Task<Result<DiscountDocument>> AddDiscount(DiscountDto discount, CancellationToken ct)
        {
            if(discount == null)
            {
                return Result.Fail<DiscountDocument>("Discount cannot be null");
            }
            if(discount.Value <= 0)
            {
                return Result.Fail<DiscountDocument>("Discount value must be greater than zero");
            }
            if(discount.Type ==Data.Enums.DiscountType.Percentage && discount.Value > 100)
            {
                return Result.Fail<DiscountDocument>("Percentage discount value cannot be greater than 100");
            }

            var product = await _context.Products.Find(p => p.ProductId == discount.ProductId).FirstOrDefaultAsync();

            if (product == null)
            {
                return Result.Fail<DiscountDocument>("Product not found");
            }

            if (discount.Type==DiscountType.FixedAmount)
            {
                if (discount.Value > product.Price)
                {
                    return Result.Fail<DiscountDocument>("Fixed amount discount value cannot be greater than product price");
                }
            }            

            var now = DateTime.UtcNow;

            var existingActiveDiscount = await _context.Discounts
                .Find(d => d.ProductId == discount.ProductId
                        && (d.StartsAt == null || d.StartsAt <= now)
                        && (d.ExpiresAt == null || d.ExpiresAt > now))
                .FirstOrDefaultAsync(ct);

            if (existingActiveDiscount != null)
            {
                return Result.Fail<DiscountDocument>(
                    "This product already has an active discount. Update it instead of adding a new one.");
            }

            var discounts = new DiscountDocument
            {
                ProductId=discount.ProductId,
                ExpiresAt=discount.ExpiresAt,
                StartsAt=discount.StartsAt,
                TimesUsed=discount.TimesUsed,
                Type=discount.Type,
                Value=discount.Value
            };
            
             await _context.Discounts.InsertOneAsync(discounts,cancellationToken: ct);

            return discounts;
        }

        public async Task<Result<bool>> DeleteDiscount(int productId, CancellationToken ct)
        {
            if(productId<=0)
            {
                return Result.Fail<bool>("Invalid discount ID");
            }

            // Discounts are kept as history for analytics, so deleting only
            // expires the current/future rows instead of removing them.
            var now = DateTime.UtcNow;
            var updateResult = await _context.Discounts.UpdateManyAsync(
                d => d.ProductId == productId && (d.ExpiresAt == null || d.ExpiresAt > now),
                Builders<DiscountDocument>.Update.Set(d => d.ExpiresAt, now),
                cancellationToken: ct);

            if (updateResult.ModifiedCount == 0)
            {
                return Result.Fail<bool>("Discount not found");
            }

            return true;
        }

        public async Task<Result<DiscountDocument>> GetDiscountByProductId(int id, CancellationToken ct)
        {
            if(id<=0)
            {
                return Result.Fail<DiscountDocument>("Invalid product ID");
            }

            var now = DateTime.UtcNow;

            var discount = await _context.Discounts
                .Find(d => d.ProductId == id
                        && (d.StartsAt == null || d.StartsAt <= now)
                        && (d.ExpiresAt == null || d.ExpiresAt > now))
                .FirstOrDefaultAsync(ct);
            if(discount == null)
            {
                return Result.Fail<DiscountDocument>("Discount not found");
            }

            return discount;
        }

        public async Task<Result<List<DiscountDocument>>> GetDiscounts(CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var discounts = await _context.Discounts
                .Find(d => (d.StartsAt == null || d.StartsAt <= now)
                        && (d.ExpiresAt == null || d.ExpiresAt > now))
                .ToListAsync(ct);

            // A product can only have one active discount; legacy duplicates
            // are collapsed to the most recent one for the API response.
            return discounts
                .GroupBy(d => d.ProductId)
                .Select(g => g.OrderByDescending(d => d.StartsAt).First())
                .ToList();
        }

        public async Task<Result<DiscountDocument>> UpdateDiscount(DiscountDto discount, CancellationToken ct)
        {
            if (discount == null)
            {
                return Result.Fail<DiscountDocument>("Invalid discount data");
            }

            var now = DateTime.UtcNow;

            var existingDiscount = await _context.Discounts
                .Find(d => d.ProductId == discount.ProductId
                        && (d.StartsAt == null || d.StartsAt <= now)
                        && (d.ExpiresAt == null || d.ExpiresAt > now))
                .FirstOrDefaultAsync(ct);

            if (existingDiscount == null)
            {
                return Result.Fail<DiscountDocument>("Discount not found");
            }

            existingDiscount.ExpiresAt = discount.ExpiresAt;
            existingDiscount.StartsAt = discount.StartsAt;
            existingDiscount.TimesUsed = discount.TimesUsed;
            existingDiscount.Type = discount.Type;
            existingDiscount.Value = discount.Value;

            await _context.Discounts.ReplaceOneAsync(d => d.Id == existingDiscount.Id, existingDiscount, cancellationToken: ct);

            return existingDiscount;
        }
    }
}
