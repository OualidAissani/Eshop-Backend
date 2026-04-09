using Eshop.Events;
using Eshop.Orders.Services.IServices;
using MassTransit;

namespace Eshop.Orders.EventHandler
{
    public class DeleteCartProductConsumer : IConsumer<DeleteCartProduct>
    {
        private readonly ICartService _cartService;
        public DeleteCartProductConsumer(ICartService cartService)
        {
            _cartService = cartService;
        }
        public async Task Consume(ConsumeContext<DeleteCartProduct> context)
        {
            var message= context.Message;

            var result = await _cartService.DeleteCartItemByProductId(message.ProductId, context.CancellationToken);
            if (result.IsFailed)
            {
                return;
            }

        }
    }
}
