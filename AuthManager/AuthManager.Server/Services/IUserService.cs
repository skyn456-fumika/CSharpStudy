using AuthManager.Server.DTOs;

namespace AuthManager.Server.Services;

public interface IUserService
{
    Task<MyProfileResponse> GetMyProfileAsync(long userId);

    Task<List<AdminUserResponse>> GetUsersAsync();

    Task<AdminUserResponse> ChangeRoleAsync(
        long userId,
        string role);

    Task<AdminUserResponse> ChangeStatusAsync(
        long userId,
        bool isActive);

    Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request);

    Task UpdateNicknameAsync(
        long userId,
        UpdateNicknameRequest request);
}