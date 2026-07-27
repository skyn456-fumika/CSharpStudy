using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AuthManager.Web.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStorageService tokenStorageService;
    private readonly ITokenRefreshService tokenRefreshService;

    private static readonly ClaimsPrincipal AnonymousUser =
        new(new ClaimsIdentity());

    public JwtAuthenticationStateProvider(
        ITokenStorageService tokenStorageService,
        ITokenRefreshService tokenRefreshService)
    {
        this.tokenStorageService = tokenStorageService;
        this.tokenRefreshService = tokenRefreshService;
    }

    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        string? accessToken =
            await tokenStorageService.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return CreateAnonymousState();
        }

        try
        {
            JwtSecurityToken jwtToken =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(accessToken);

            if (jwtToken.ValidTo <= DateTime.UtcNow)
            {
                accessToken =
                    await tokenRefreshService
                        .RefreshAccessTokenAsync();

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    await tokenStorageService
                        .RemoveTokensAsync();

                    return CreateAnonymousState();
                }

                jwtToken =
                    new JwtSecurityTokenHandler()
                        .ReadJwtToken(accessToken);
            }

            return CreateAuthenticatedState(jwtToken);
        }
        catch
        {
            await tokenStorageService.RemoveTokensAsync();

            return CreateAnonymousState();
        }
    }

    public void NotifyUserAuthentication(string accessToken)
    {
        try
        {
            JwtSecurityToken jwtToken =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(accessToken);

            AuthenticationState state =
                CreateAuthenticatedState(jwtToken);

            NotifyAuthenticationStateChanged(
                Task.FromResult(state));
        }
        catch
        {
            NotifyAuthenticationStateChanged(
                Task.FromResult(CreateAnonymousState()));
        }
    }

    public async Task ForceLogoutAsync()
    {
        await tokenStorageService.RemoveTokensAsync();

        NotifyAuthenticationStateChanged(
            Task.FromResult(CreateAnonymousState()));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(
            Task.FromResult(CreateAnonymousState()));
    }

    private static AuthenticationState
        CreateAuthenticatedState(JwtSecurityToken jwtToken)
    {
        ClaimsIdentity identity = new(
            jwtToken.Claims,
            authenticationType: "jwt",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        ClaimsPrincipal user = new(identity);

        return new AuthenticationState(user);
    }

    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(AnonymousUser);
    }
}