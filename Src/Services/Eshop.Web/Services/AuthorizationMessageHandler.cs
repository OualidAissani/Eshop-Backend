using System.Net.Http.Headers;

namespace Eshop.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorage;

    public AuthorizationMessageHandler(ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStorage.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Optional: Handle 401 and trigger token refresh
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // You can implement token refresh logic here
        }

        return response;
    }
}
