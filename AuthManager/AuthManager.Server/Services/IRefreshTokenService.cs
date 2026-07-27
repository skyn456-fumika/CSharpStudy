using AuthManager.Server.Entities;

namespace AuthManager.Server.Services;

public interface IRefreshTokenService
{
    Task<(string Token, DateTime ExpiresAt)> CreateAsync(
        AppUser user);

    Task<AppUser> ValidateAndRevokeAsync(
        string rawToken);

    Task RevokeAsync(string rawToken);
}