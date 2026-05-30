using Eshop.Catalog.Data;
using Eshop.Events;
using MassTransit;
using MongoDB.Driver;

namespace Eshop.Catalog.EventsHandler
{
    public class VerifyProductExistenceConsumer:IConsumer<VerifyProductExistence>
    {
        private readonly MongoCatalogContext _db;
        public VerifyProductExistenceConsumer(MongoCatalogContext catalogDbContext)
        {
            _db = catalogDbContext;
        }
        public async Task Consume(ConsumeContext<VerifyProductExistence> Context)
        {
            var message=Context.Message;
            var exists = await _db.Products.Find(i => i.ProductId == message.ProductId).AnyAsync(Context.CancellationToken);

            await Context.RespondAsync(new ProductExistenceResponse(exists));
        }
    }
}
