using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthManager.Server.DTOs;
using AuthManager.Server.Entities;
using AuthManager.Server.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthManager.Server.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        jwtSettings = jwtOptions.Value;
    }

    public AccessTokenResult CreateAccessToken(AppUser user)
    {
        DateTime expiresAt = DateTime.UtcNow
            .AddMinutes(jwtSettings.ExpirationMinutes);

        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.UniqueName,
                user.Username),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                "nickname",
                user.Nickname),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        ];

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(jwtSettings.Key));

        SigningCredentials credentials = new(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessTokenResult
        {
            AccessToken =
                new JwtSecurityTokenHandler().WriteToken(token),

            ExpiresAt = expiresAt
        };
    }
}