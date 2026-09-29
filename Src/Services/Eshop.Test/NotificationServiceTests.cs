using Eshop.Notification.Services;
using Eshop.Orders.Services;
using Imposter;
using Imposter.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;



namespace Eshop.Test;
public class NotificationServiceTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configurations;


    public NotificationServiceTests()
    {
        _handler = new FakeHttpMessageHandler();
        var httpClientFactoryImposter =  IHttpClientFactory.Imposter();
        httpClientFactoryImposter.CreateClient(Arg<string>.Any()).Returns(new HttpClient(_handler));

        _configurations=new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailService:BaseUrl"] = "https://api.emailservice.com",
                ["EmailService:ApiKey"] = "test_api_key",
                ["EmailService:SenderEmail"] = "walid@gmail.com",
                ["EmailService:SenderName"] = "Eshop"
            })
            .Build();

        _emailService = new EmailService(httpClientFactoryImposter.Instance(),_configurations);
        
    }

    public void Dispose()
    {
        _handler?.Dispose();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSendEmailSuccessfully()
    {
        var to = "emailtest@gmail.com";
        var subject = "suvject text";
        var body = "booody";

        await _emailService.SendEmailAsync(to,subject,body,CancellationToken.None);
    }
}
