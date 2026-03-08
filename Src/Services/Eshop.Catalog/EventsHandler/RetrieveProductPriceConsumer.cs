using MassTransit;
using Eshop.Events;
using Eshop.Catalog.Services.IServices;

namespace Eshop.Catalog.EventsHandler
{
    public class RetrieveProductPriceConsumer : IConsumer<GetProductRequest>
    {
        private readonly IProductService _ProductService;
        public RetrieveProductPriceConsumer(IProductService productRepository)
        {
            _ProductService= productRepository;
        }
        public async Task Consume(ConsumeContext<GetProductRequest> context)
        {
            var message = context.Message;
            var price= await _ProductService.GetProductPrice(message.ProductId);
            var response = new GetProductResponse(price.Select(i => new GetProductResponseDto(i.Id, i.Price,i.Name)).ToList());
            await context.RespondAsync(response);
        }
    }
}
