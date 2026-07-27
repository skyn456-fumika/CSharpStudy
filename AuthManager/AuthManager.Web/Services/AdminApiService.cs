using System.Net;
using System.Net.Http.Json;
using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public class AdminApiService : IAdminApiService
{
    private readonly HttpClient httpClient;

    public AdminApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<AdminUserResponse>> GetUsersAsync()
    {
        HttpResponseMessage response =
            await httpClient.GetAsync("api/admin/users");

        if (response.IsSuccessStatusCode)
        {
            List<AdminUserResponse>? result =
                await response.Content
                    .ReadFromJsonAsync<List<AdminUserResponse>>();

            return result ?? [];
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "로그인이 필요하거나 토큰이 만료되었습니다.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "관리자 권한이 필요합니다.");
        }

        throw new HttpRequestException(
            "사용자 목록 조회에 실패했습니다.");
    }

    public async Task<AdminUserResponse> ChangeRoleAsync(
        long userId,
        string role)
    {
        ChangeRoleRequest request = new()
        {
            Role = role
        };

        HttpResponseMessage response =
            await httpClient.PatchAsJsonAsync(
                $"api/admin/users/{userId}/role",
                request);

        if (response.IsSuccessStatusCode)
        {
            AdminUserResponse? result =
                await response.Content
                    .ReadFromJsonAsync<AdminUserResponse>();

            return result ?? throw new InvalidOperationException(
                "역할 변경 응답을 읽을 수 없습니다.");
        }

        ApiErrorResponse? error = null;

        try
        {
            error = await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();
        }
        catch
        {
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "로그인이 필요하거나 토큰이 만료되었습니다.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "관리자 권한이 필요합니다.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException(
                error?.Message ?? "사용자를 찾을 수 없습니다.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                error?.Message ?? "잘못된 역할입니다.");
        }

        throw new HttpRequestException(
            error?.Message ?? "역할 변경에 실패했습니다.");
    }

    public async Task<AdminUserResponse> ChangeStatusAsync(
        long userId,
        bool isActive)
    {
        ChangeUserStatusRequest request = new()
        {
            IsActive = isActive
        };

        HttpResponseMessage response =
            await httpClient.PatchAsJsonAsync(
                $"api/admin/users/{userId}/status",
                request);

        if (response.IsSuccessStatusCode)
        {
            AdminUserResponse? result =
                await response.Content
                    .ReadFromJsonAsync<AdminUserResponse>();

            return result ?? throw new InvalidOperationException(
                "상태 변경 응답을 읽을 수 없습니다.");
        }

        ApiErrorResponse? error = null;

        try
        {
            error = await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();
        }
        catch
        {
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                error?.Message ?? "상태를 변경할 수 없습니다.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException(
                error?.Message ?? "사용자를 찾을 수 없습니다.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "관리자 권한이 필요합니다.");
        }

        throw new HttpRequestException(
            error?.Message ?? "사용자 상태 변경에 실패했습니다.");
    }
}