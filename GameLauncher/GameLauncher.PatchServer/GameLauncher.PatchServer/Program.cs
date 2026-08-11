var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string patchRoot = Path.GetFullPath(
    Path.Combine(
        app.Environment.ContentRootPath,
        "..",
        "..",
        "PatchServer"));

string filesRoot = Path.Combine(
    patchRoot,
    "files");

app.MapGet("/", () =>
{
    return "GameLauncher Patch Server";
});

app.MapGet("/version.json", () =>
{
    string path = Path.Combine(
        patchRoot,
        "version.json");

    if (!File.Exists(path))
    {
        return Results.NotFound();
    }

    return Results.File(
        path,
        "application/json");
});

app.MapGet("/manifest.json", () =>
{
    string path = Path.Combine(
        patchRoot,
        "manifest.json");

    if (!File.Exists(path))
    {
        return Results.NotFound();
    }

    return Results.File(
        path,
        "application/json");
});

app.MapGet("/files/{**filePath}", (string filePath) =>
{
    string requestedPath = Path.GetFullPath(
        Path.Combine(
            filesRoot,
            filePath));

    string normalizedFilesRoot =
        Path.GetFullPath(filesRoot)
        + Path.DirectorySeparatorChar;

    if (!requestedPath.StartsWith(
        normalizedFilesRoot,
        StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    if (!File.Exists(requestedPath))
    {
        return Results.NotFound();
    }

    return Results.File(
        requestedPath,
        "application/octet-stream");
});

Console.WriteLine($"ContentRootPath: {app.Environment.ContentRootPath}");
Console.WriteLine($"PatchRoot: {patchRoot}");

app.Run();