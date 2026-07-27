using AuthManager.Web;
using AuthManager.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder =
    WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();

builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddScoped<AuthHeaderHandler>();

builder.Services
    .AddHttpClient("AuthManagerApi", client =>
    {
        client.BaseAddress =
            new Uri("https://localhost:7293/");
    })
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient("RefreshApi", client =>
{
    client.BaseAddress =
        new Uri("https://localhost:7293/");
});

builder.Services.AddScoped<
    ITokenRefreshService,
    TokenRefreshService>();

builder.Services.AddScoped<HttpClient>(serviceProvider =>
{
    IHttpClientFactory factory =
        serviceProvider.GetRequiredService<IHttpClientFactory>();

    return factory.CreateClient("AuthManagerApi");
});


builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IAdminApiService, AdminApiService>();

builder.Services.AddScoped<JwtAuthenticationStateProvider>();

builder.Services.AddScoped<AuthenticationStateProvider>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            JwtAuthenticationStateProvider>());

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();