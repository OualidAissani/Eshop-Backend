using Eshop.Payement.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Eshop.Payement.Services.IServices
{
    public interface IPaymentService
    {
        Task<string> GetAccessToken();

        Task<string> CreateOrder(List<Models.ItemsDto> items, Models.AmountDto amount);

        Task<int> CapturePayment(string orderId, string userId);

        Task<JsonElement> GetCaptureDetails(string captureId);

        Task<JsonElement> GetOrderDetails(string orderId);

        Task<object> RefundPayment(string captureId, AmountDto? amount,string userId);
        //Task<int> Webhook(string payload);
    }
}
