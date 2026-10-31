using Eshop.Orders.Data;
using Eshop.Orders.Data.Enums;
using Eshop.Orders.Dtos;
using Eshop.Orders.Entities;
using Eshop.Orders.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Orders.Services;

    public class DeliveryPriceService : IDeliveryPriceService
    {
        private const int MinWilayaCode = 1;
        private const int MaxWilayaCode = 58;

        private readonly OrderDbContext _context;

        public DeliveryPriceService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<decimal?> GetPriceAsync(int wilayaCode, DeliveryType deliveryType, CancellationToken ct)
        {
            if (wilayaCode is < MinWilayaCode or > MaxWilayaCode)
            {
                return null;
            }

            var price = await _context.DeliveryPrices
                .AsNoTracking()
                .Where(p => p.WilayaCode == wilayaCode && p.DeliveryType == deliveryType)
                .Select(p => (decimal?)p.PriceDzd)
                .FirstOrDefaultAsync(ct);

            return price is > 0 ? price : null;
        }

        public async Task<List<DeliveryPriceDto>> GetAllAsync(CancellationToken ct)
        {
            return await _context.DeliveryPrices
                .AsNoTracking()
                .OrderBy(p => p.WilayaCode)
                .ThenBy(p => p.DeliveryType)
                .Select(p => new DeliveryPriceDto
                {
                    WilayaCode = p.WilayaCode,
                    DeliveryType = p.DeliveryType,
                    PriceDzd = p.PriceDzd
                })
                .ToListAsync(ct);
        }

        public async Task UpsertAsync(IEnumerable<DeliveryPriceDto> prices, CancellationToken ct)
        {
            var rows = prices?.ToList() ?? [];

            if (rows.Count == 0)
            {
                return;
            }

            foreach (var row in rows)
            {
                if (row.WilayaCode is < MinWilayaCode or > MaxWilayaCode)
                {
                    throw new ArgumentException($"Invalid wilaya code: {row.WilayaCode}. Expected {MinWilayaCode}..{MaxWilayaCode}.");
                }

                if (row.PriceDzd < 0)
                {
                    throw new ArgumentException($"Price cannot be negative for wilaya {row.WilayaCode}.");
                }
            }

            var wilayaCodes = rows.Select(p => p.WilayaCode).Distinct().ToList();

            var existing = await _context.DeliveryPrices
                .Where(p => wilayaCodes.Contains(p.WilayaCode))
                .ToListAsync(ct);

            try
            {
                
            foreach (var row in rows)
            {
                var entity = existing.FirstOrDefault(p =>
                    p.WilayaCode == row.WilayaCode && p.DeliveryType == row.DeliveryType);

                // A zero price means "not configured": remove the row so the
                // checkout reports the route as unavailable instead of free.
                if (row.PriceDzd == 0)
                {
                    if (entity is not null)
                    {
                        _context.DeliveryPrices.Remove(entity);
                    }

                    continue;
                }

                if (entity is null)
                {
                    _context.DeliveryPrices.Add(new DeliveryPrice
                    {
                        WilayaCode = row.WilayaCode,
                        DeliveryType = row.DeliveryType,
                        PriceDzd = row.PriceDzd
                    });
                }
                else
                {
                    entity.PriceDzd = row.PriceDzd;
                }
            }

            await _context.SaveChangesAsync(ct);
            }
                     
            catch(Exception ex)
            {
                return;
            }
    }
}

