using System.Security.Cryptography;
using System.Text.Json;

if (args.Length < 3)
{
    Console.WriteLine(
        "사용법: GameLauncher.ManifestGenerator <빌드폴더> <출력폴더> <버전>");

    return;
}

string buildDirectory = args[0];
string outputDirectory = args[1];
string version = args[2];

if (!Directory.Exists(buildDirectory))
{
    Console.WriteLine(
        $"빌드 폴더를 찾을 수 없습니다: {buildDirectory}");

    return;
}

Directory.CreateDirectory(outputDirectory);

string filesDirectory = Path.Combine(
    outputDirectory,
    "files");

Console.WriteLine("배포 파일을 동기화하고 있습니다...");

SyncBuildFiles(
    buildDirectory,
    filesDirectory);

Console.WriteLine("Manifest를 생성하고 있습니다...");

List<ManifestFile> files = [];

foreach (string filePath in Directory.EnumerateFiles(
    filesDirectory,
    "*",
    SearchOption.AllDirectories))
{
    string relativePath = Path.GetRelativePath(
        filesDirectory,
        filePath);

    relativePath = relativePath.Replace('\\', '/');

    FileInfo fileInfo = new FileInfo(filePath);

    string sha256 = CalculateSha256(filePath);

    files.Add(new ManifestFile
    {
        Path = relativePath,
        Size = fileInfo.Length,
        Hash = sha256
    });
}

files = files
    .OrderBy(file => file.Path)
    .ToList();

JsonSerializerOptions options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

Manifest manifest = new Manifest
{
    Version = version,
    Files = files
};

string manifestPath = Path.Combine(
    outputDirectory,
    "manifest.json");

string manifestJson = JsonSerializer.Serialize(
    manifest,
    options);

File.WriteAllText(
    manifestPath,
    manifestJson);

GameVersionInfo versionInfo = new GameVersionInfo
{
    Version = version
};

string versionPath = Path.Combine(
    outputDirectory,
    "version.json");

string versionJson = JsonSerializer.Serialize(
    versionInfo,
    options);

File.WriteAllText(
    versionPath,
    versionJson);

Console.WriteLine();
Console.WriteLine("배포 준비 완료");
Console.WriteLine($"버전: {version}");
Console.WriteLine($"파일 수: {files.Count}");
Console.WriteLine($"Files: {filesDirectory}");
Console.WriteLine($"Manifest: {manifestPath}");
Console.WriteLine($"Version: {versionPath}");

static void SyncBuildFiles(
    string sourceDirectory,
    string targetDirectory)
{
    if (Directory.Exists(targetDirectory))
    {
        Directory.Delete(
            targetDirectory,
            true);
    }

    Directory.CreateDirectory(targetDirectory);

    foreach (string filePath in Directory.EnumerateFiles(
        sourceDirectory,
        "*",
        SearchOption.AllDirectories))
    {
        string relativePath = Path.GetRelativePath(
            sourceDirectory,
            filePath);

        relativePath = relativePath.Replace('\\', '/');

        if (ShouldExclude(relativePath))
            continue;

        string targetPath = Path.Combine(
            targetDirectory,
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar));

        string? targetParent =
            Path.GetDirectoryName(targetPath);

        if (!string.IsNullOrWhiteSpace(targetParent))
        {
            Directory.CreateDirectory(targetParent);
        }

        File.Copy(
            filePath,
            targetPath,
            true);
    }
}

static bool ShouldExclude(string relativePath)
{
    string normalizedPath =
        relativePath.Replace('\\', '/');

    string fileName =
        Path.GetFileName(normalizedPath);

    // 런처가 별도로 관리하는 메타 파일
    if (fileName.Equals(
        "version.json",
        StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (fileName.Equals(
        "manifest.json",
        StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    // 테스트/임시 파일
    if (fileName.EndsWith(
        ".bak",
        StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (fileName.EndsWith(
        ".tmp",
        StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    // Unity 디버그 전용 폴더
    string[] directories =
        normalizedPath.Split('/');

    if (directories.Any(directory =>
        directory.EndsWith(
            "BurstDebugInformation_DoNotShip",
            StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return false;
}

static string CalculateSha256(string filePath)
{
    using FileStream stream = File.OpenRead(filePath);

    byte[] hash = SHA256.HashData(stream);

    return Convert.ToHexString(hash);
}

public class Manifest
{
    public string Version { get; set; } = string.Empty;

    public List<ManifestFile> Files { get; set; } = [];
}

public class ManifestFile
{
    public string Path { get; set; } = string.Empty;

    public long Size { get; set; }

    public string Hash { get; set; } = string.Empty;
}

public class GameVersionInfo
{
    public string Version { get; set; } = string.Empty;
}