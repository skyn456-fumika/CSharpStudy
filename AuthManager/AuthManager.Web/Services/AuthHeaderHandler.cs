using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthManager.Web.Models;

namespace AuthManager.Web.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStorageService tokenStorageService;
    private readonly ITokenRefreshService tokenRefreshService;

    public AuthHeaderHandler(
        ITokenStorageService tokenStorageService,
        ITokenRefreshService tokenRefreshService)
    {
        this.tokenStorageService = tokenStorageService;
        this.tokenRefreshService = tokenRefreshService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? accessToken =
            await tokenStorageService.GetAccessTokenAsync();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        HttpRequestMessage retryRequest =
            await CloneRequestAsync(
                request,
                cancellationToken);

        HttpResponseMessage response =
            await base.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            string? newAccessToken =
                await tokenRefreshService
                    .RefreshAccessTokenAsync();

            if (!string.IsNullOrWhiteSpace(newAccessToken))
            {
                response.Dispose();

                retryRequest.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        newAccessToken);

                return await base.SendAsync(
                    retryRequest,
                    cancellationToken);
            }

            await tokenStorageService.RemoveTokensAsync();
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            ApiErrorResponse? error = null;

            try
            {
                error = await response.Content
                    .ReadFromJsonAsync<ApiErrorResponse>(
                        cancellationToken: cancellationToken);
            }
            catch
            {
            }

            if (error?.Code == "ACCOUNT_INACTIVE")
            {
                await tokenStorageService.RemoveTokensAsync();
            }
        }

        retryRequest.Dispose();

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpRequestMessage clone =
            new(request.Method, request.RequestUri);

        foreach (KeyValuePair<string, IEnumerable<string>> header
                 in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        if (request.Content != null)
        {
            byte[] contentBytes =
                await request.Content.ReadAsByteArrayAsync(
                    cancellationToken);

            clone.Content = new ByteArrayContent(contentBytes);

            foreach (
                KeyValuePair<string, IEnumerable<string>> header
                in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }
        }

        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;

        return clone;
    }
}