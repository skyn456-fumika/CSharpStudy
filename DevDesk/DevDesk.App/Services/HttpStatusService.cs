using System.Diagnostics;
using System.Net.Http;
using DevDesk.App.Models;

namespace DevDesk.App.Services;

public class HttpStatusService
{
    private readonly HttpClient _httpClient;

    public HttpStatusService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<HttpCheckResultModel> CheckAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "http:// 또는 https://로 시작하는 올바른 주소를 입력하세요.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            return new HttpCheckResultModel
            {
                Url = url,
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.Now,
                Message = response.IsSuccessStatusCode
                    ? "서버가 정상적으로 응답했습니다."
                    : $"서버가 오류 상태로 응답했습니다: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();

            return new HttpCheckResultModel
            {
                Url = url,
                IsSuccess = false,
                StatusCode = null,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.Now,
                Message = ex is TaskCanceledException
                    ? "응답 제한 시간 10초를 초과했습니다."
                    : ex.Message
            };
        }
    }
}