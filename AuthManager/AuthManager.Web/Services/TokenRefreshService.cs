using System.Net.Http.Json;
using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public class TokenRefreshService : ITokenRefreshService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ITokenStorageService tokenStorageService;

    public TokenRefreshService(
        IHttpClientFactory httpClientFactory,
        ITokenStorageService tokenStorageService)
    {
        this.httpClientFactory = httpClientFactory;
        this.tokenStorageService = tokenStorageService;
    }

    public async Task<string?> RefreshAccessTokenAsync()
    {
        string? refreshToken =
            await tokenStorageService.GetRefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        HttpClient client =
            httpClientFactory.CreateClient("RefreshApi");

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                });

        if (!response.IsSuccessStatusCode)
        {
            await tokenStorageService.RemoveTokensAsync();
            return null;
        }

        LoginResponse? result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result == null)
        {
            await tokenStorageService.RemoveTokensAsync();
            return null;
        }

        await tokenStorageService.SaveTokensAsync(
            result.AccessToken,
            result.RefreshToken);

        return result.AccessToken;
    }
}