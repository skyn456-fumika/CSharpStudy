using AuthManager.Web.Models;
using System.Net;
using System.Net.Http.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AuthManager.Web.Services;

public class UserApiService : IUserApiService
{
    private readonly HttpClient httpClient;

    public UserApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<MyProfileResponse> GetMyProfileAsync()
    {
        HttpResponseMessage response =
            await httpClient.GetAsync("api/users/me");

        if (response.IsSuccessStatusCode)
        {
            MyProfileResponse? result =
                await response.Content
                    .ReadFromJsonAsync<MyProfileResponse>();

            return result ?? throw new InvalidOperationException(
                "내 정보 응답을 읽을 수 없습니다.");
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
                error?.Message ?? "접근할 수 없는 계정입니다.");
        }

        throw new HttpRequestException(
            "내 정보 조회에 실패했습니다.");
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request)
    {
        HttpResponseMessage response =
            await httpClient.PatchAsJsonAsync(
                "api/users/me/password",
                request);

        if (response.IsSuccessStatusCode)
        {
            return;
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

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                error?.Message ??
                "비밀번호 변경 요청이 올바르지 않습니다.");
        }

        throw new HttpRequestException(
            error?.Message ??
            "비밀번호 변경에 실패했습니다.");
    }

    public async Task UpdateNicknameAsync(
    UpdateNicknameRequest request)
    {
        HttpResponseMessage response =
            await httpClient.PatchAsJsonAsync(
                "api/users/me/nickname",
                request);

        if (response.IsSuccessStatusCode)
        {
            return;
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
                error?.Message ??
                "닉네임 변경 요청이 올바르지 않습니다.");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                error?.Message ??
                "이미 사용 중인 닉네임입니다.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "로그인이 필요합니다.");
        }

        throw new HttpRequestException(
            error?.Message ??
            "닉네임 변경에 실패했습니다.");
    }
}