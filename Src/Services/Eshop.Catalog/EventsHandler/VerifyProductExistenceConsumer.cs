using Eshop.Catalog.Data;
using Eshop.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Catalog.EventsHandler
{
    public class VerifyProductExistenceConsumer:IConsumer<VerifyProductExistence>
    {
        private readonly CatalogDbContext _db;
        public VerifyProductExistenceConsumer(CatalogDbContext catalogDbContext)
        {
            _db = catalogDbContext;
        }
        public async Task Consume(ConsumeContext<VerifyProductExistence> Context)
        {
            var message=Context.Message;
            var exists=await _db.Products.AnyAsync(i=>i.Id==message.ProductId);

            await Context.RespondAsync(new ProductExistenceResponse(exists));
        }
    }
}
