using AuthManager.Server.DTOs;
using AuthManager.Server.Entities;

namespace AuthManager.Server.Services;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(AppUser user);
}