using System.Net;

namespace Eshop.Test;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> predicate, Func<HttpResponseMessage> factory)> _rules = [];

    public FakeHttpMessageHandler When(HttpMethod method, string urlContains, Func<HttpResponseMessage> responseFactory)
    {
        _rules.Add((
            req => req.Method == method
                && req.RequestUri!.ToString().Contains(urlContains, StringComparison.OrdinalIgnoreCase),
            responseFactory));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var (predicate, factory) in _rules)
        {
            if (predicate(request))
                return Task.FromResult(factory());
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
