using AuthManager.Server.Data;
using AuthManager.Server.DTOs;
using AuthManager.Server.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthManager.Server.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext dbContext;
    private readonly IPasswordHasher<AppUser> passwordHasher;
    private readonly IJwtTokenService jwtTokenService;
    private readonly IRefreshTokenService refreshTokenService;

    public AuthService(
        AuthDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.jwtTokenService = jwtTokenService;
        this.refreshTokenService = refreshTokenService;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        string username = request.Username.Trim();
        string nickname = request.Nickname.Trim();

        bool usernameExists = await dbContext.Users
            .AnyAsync(user => user.Username == username);

        if (usernameExists)
        {
            throw new InvalidOperationException("이미 사용 중인 아이디입니다.");
        }

        AppUser user = new()
        {
            Username = username,
            Nickname = nickname,
            Role = "USER",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return new RegisterResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        string username = request.Username.Trim();

        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Username == username);

        if (user == null)
        {
            throw new UnauthorizedAccessException(
                "아이디 또는 비밀번호가 올바르지 않습니다.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "정지된 계정입니다.");
        }

        PasswordVerificationResult verificationResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "아이디 또는 비밀번호가 올바르지 않습니다.");
        }

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(
                user,
                request.Password);

            await dbContext.SaveChangesAsync();
        }

        AccessTokenResult accessToken =
            jwtTokenService.CreateAccessToken(user);

        (string refreshToken, DateTime refreshTokenExpiresAt) =
            await refreshTokenService.CreateAsync(user);

        return new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAt = accessToken.ExpiresAt,

            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,

            UserId = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role
        };
    }

    public async Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request)
    {
        AppUser user =
            await refreshTokenService.ValidateAndRevokeAsync(
                request.RefreshToken);

        AccessTokenResult accessToken =
            jwtTokenService.CreateAccessToken(user);

        (string refreshToken, DateTime refreshTokenExpiresAt) =
            await refreshTokenService.CreateAsync(user);

        return new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAt = accessToken.ExpiresAt,

            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,

            UserId = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role
        };
    }

    public async Task LogoutAsync(LogoutRequest request)
    {
        await refreshTokenService.RevokeAsync(
            request.RefreshToken);
    }
}