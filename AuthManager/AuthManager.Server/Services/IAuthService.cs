using AuthManager.Server.DTOs;

namespace AuthManager.Server.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(
        RegisterRequest request);

    Task<LoginResponse> LoginAsync(
        LoginRequest request);

    Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request);

    Task LogoutAsync(LogoutRequest request);
}