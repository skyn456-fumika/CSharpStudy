using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 테스트용 10MB 패치 파일
byte[] configData = new byte[10 * 1024 * 1024];

string configHash = Convert.ToHexString(
    SHA256.HashData(configData));

app.MapGet("/", () => "GameLauncher Patch Server");

app.MapGet("/version.json", () =>
{
    return Results.Json(new
    {
        Version = "1.4.0"
    });
});

app.MapGet("/manifest.json", () =>
{
    return Results.Json(new
    {
        Version = "1.4.0",

        Files = new[]
        {
            new
            {
                Path = "TestGameServer.exe",    // GameServerManager 프로젝트의 테스트 실행 파일을 게임 exe로 사용
                Size = 156160L,
                Hash = ""
            },
            new
            {
                Path = "Data/config.dat",
                Size = configData.LongLength,
                Hash = configHash
            }
        }
    });
});

app.MapGet("/files/Data/config.dat", () =>
{
    return Results.File(
        configData,
        "application/octet-stream",
        "config.dat");
});

app.Run();