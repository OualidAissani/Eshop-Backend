using Eshop.Events;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using MassTransit;

namespace Eshop.Orders.EventHandler
{
    public class UpdateCartProductConsumer : IConsumer<UpdateCartProduct>
    {
        private readonly ICartService _cartService;

        public UpdateCartProductConsumer(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task Consume(ConsumeContext<UpdateCartProduct> context)
        {
            var message = context.Message;
            var updateCartItemDto = new UpdateCartItemDto()
            {
                ProductId = message.ProductId,
                FullPrice = message.FullPrice,
                ProductName = message.ProductName
            };
            if (await _cartService.UpdateCartItem(updateCartItemDto, context.CancellationToken) == null)
            {

                return;
            }
        }
    }
}

