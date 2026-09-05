namespace Eshop.Orders.Services.IServices
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken ct);
    }
}
