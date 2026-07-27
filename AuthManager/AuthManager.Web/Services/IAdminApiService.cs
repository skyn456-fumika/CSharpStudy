using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public interface IAdminApiService
{
    Task<List<AdminUserResponse>> GetUsersAsync();

    Task<AdminUserResponse> ChangeRoleAsync(
        long userId,
        string role);

    Task<AdminUserResponse> ChangeStatusAsync(
        long userId,
        bool isActive);
}