using Microsoft.JSInterop;

namespace AuthManager.Web.Services;

public class TokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey =
        "auth_access_token";

    private const string RefreshTokenKey =
        "auth_refresh_token";

    private readonly IJSRuntime jsRuntime;

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async Task SaveTokensAsync(
        string accessToken,
        string refreshToken)
    {
        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            AccessTokenKey,
            accessToken);

        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            RefreshTokenKey,
            refreshToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            RefreshTokenKey);
    }

    public async Task RemoveTokensAsync()
    {
        await jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            AccessTokenKey);

        await jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            RefreshTokenKey);
    }
}