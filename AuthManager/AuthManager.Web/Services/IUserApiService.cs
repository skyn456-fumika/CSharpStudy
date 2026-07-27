using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public interface IUserApiService
{
    Task<MyProfileResponse> GetMyProfileAsync();

    Task ChangePasswordAsync(
        ChangePasswordRequest request);

    Task UpdateNicknameAsync(
        UpdateNicknameRequest request);
}