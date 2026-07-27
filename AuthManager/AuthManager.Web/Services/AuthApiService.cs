using System.Net;
using System.Net.Http.Json;
using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public class AuthApiService : IAuthApiService
{
    private readonly HttpClient httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "api/auth/register",
            request);

        if (response.IsSuccessStatusCode)
        {
            RegisterResponse? result =
                await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return result ?? throw new InvalidOperationException(
                "회원가입 응답을 읽을 수 없습니다.");
        }

        ApiErrorResponse? error =
            await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                error?.Message ?? "이미 사용 중인 아이디입니다.");
        }

        throw new HttpRequestException(
            error?.Message ?? "회원가입 요청에 실패했습니다.");
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "api/auth/login",
            request);

        if (response.IsSuccessStatusCode)
        {
            LoginResponse? result =
                await response.Content.ReadFromJsonAsync<LoginResponse>();

            return result ?? throw new InvalidOperationException(
                "로그인 응답을 읽을 수 없습니다.");
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
                error?.Message ??
                "아이디 또는 비밀번호가 올바르지 않습니다.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                error?.Message ?? "정지된 계정입니다.");
        }

        throw new HttpRequestException(
            error?.Message ?? "로그인 요청에 실패했습니다.");
    }

    public async Task<LoginResponse> RefreshAsync(
        string refreshToken)
    {
        RefreshTokenRequest request = new()
        {
            RefreshToken = refreshToken
        };

        HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                "api/auth/refresh",
                request);

        if (response.IsSuccessStatusCode)
        {
            LoginResponse? result =
                await response.Content
                    .ReadFromJsonAsync<LoginResponse>();

            return result ?? throw new InvalidOperationException(
                "토큰 재발급 응답을 읽을 수 없습니다.");
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
                error?.Message ??
                "로그인 정보가 만료되었습니다.");
        }

        throw new HttpRequestException(
            error?.Message ??
            "토큰 재발급에 실패했습니다.");
    }

    public async Task LogoutAsync(string refreshToken)
    {
        LogoutRequest request = new()
        {
            RefreshToken = refreshToken
        };

        HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                "api/auth/logout",
                request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "서버 로그아웃 처리에 실패했습니다.");
        }
    }
}