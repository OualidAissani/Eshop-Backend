using Eshop.Notification.Services;
using Eshop.Orders.Services;
using Imposter;
using Imposter.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;



namespace Eshop.Test;
internal class NotificationServiceTests : IDisposable
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
        throw new NotImplementedException();
    }
}
