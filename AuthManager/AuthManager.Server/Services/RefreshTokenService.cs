using System.Security.Cryptography;
using System.Text;
using AuthManager.Server.Data;
using AuthManager.Server.Entities;
using AuthManager.Server.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthManager.Server.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AuthDbContext dbContext;
    private readonly JwtSettings jwtSettings;

    public RefreshTokenService(
        AuthDbContext dbContext,
        IOptions<JwtSettings> jwtOptions)
    {
        this.dbContext = dbContext;
        jwtSettings = jwtOptions.Value;
    }

    public async Task<(string Token, DateTime ExpiresAt)> CreateAsync(
        AppUser user)
    {
        string rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        string tokenHash = HashToken(rawToken);

        DateTime expiresAt = DateTime.UtcNow
            .AddDays(jwtSettings.RefreshExpirationDays);

        RefreshToken refreshToken = new()
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        return (rawToken, expiresAt);
    }

    public async Task<AppUser> ValidateAndRevokeAsync(
        string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new UnauthorizedAccessException(
                "Refresh Token이 없습니다.");
        }

        string tokenHash = HashToken(rawToken);

        RefreshToken? refreshToken =
            await dbContext.RefreshTokens
                .Include(token => token.User)
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException(
                "유효하지 않은 Refresh Token입니다.");
        }

        if (refreshToken.IsRevoked)
        {
            throw new UnauthorizedAccessException(
                "이미 사용되었거나 폐기된 Refresh Token입니다.");
        }

        if (refreshToken.IsExpired)
        {
            throw new UnauthorizedAccessException(
                "Refresh Token이 만료되었습니다.");
        }

        if (!refreshToken.User.IsActive)
        {
            throw new UnauthorizedAccessException(
                "정지된 계정입니다.");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return refreshToken.User;
    }

    private static string HashToken(string token)
    {
        byte[] hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hashBytes);
    }

    public async Task RevokeAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        string tokenHash = HashToken(rawToken);

        RefreshToken? refreshToken =
            await dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash);

        if (refreshToken == null ||
            refreshToken.IsRevoked)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
}