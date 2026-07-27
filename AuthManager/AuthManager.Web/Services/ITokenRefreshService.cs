namespace AuthManager.Web.Services;

public interface ITokenRefreshService
{
    Task<string?> RefreshAccessTokenAsync();
}