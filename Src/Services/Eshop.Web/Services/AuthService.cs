using System.Net.Http.Json;
using Eshop.Auth.Models;
using Eshop.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Eshop.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStorageService _tokenStorage;
        private readonly JwtAuthenticationStateProvider _authStateProvider;

        public AuthService(
            HttpClient httpClient,
            ITokenStorageService tokenStorage,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
            _authStateProvider = (JwtAuthenticationStateProvider)authStateProvider;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", 
                new { Email = email, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                await _tokenStorage.SetTokensAsync(result!.accessToken, result.refreshToken);
                _authStateProvider.NotifyAuthenticationStateChanged();
                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            await _tokenStorage.ClearTokensAsync();
            _authStateProvider.NotifyAuthenticationStateChanged();
        }
    }
}
