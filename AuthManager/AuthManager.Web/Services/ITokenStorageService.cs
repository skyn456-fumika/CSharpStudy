namespace AuthManager.Web.Services;

public interface ITokenStorageService
{
    Task SaveTokensAsync(
        string accessToken,
        string refreshToken);

    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();

    Task RemoveTokensAsync();
}