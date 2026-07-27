using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public interface IAuthApiService
{
    Task<RegisterResponse> RegisterAsync(
        RegisterRequest request);

    Task<LoginResponse> LoginAsync(
        LoginRequest request);

    Task<LoginResponse> RefreshAsync(
        string refreshToken);

    Task LogoutAsync(string refreshToken);
}