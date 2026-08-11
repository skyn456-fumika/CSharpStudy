using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using GameLauncher.App.Models;

namespace GameLauncher.App.Services
{
    public class PatchService
    {
        private readonly HttpClient _httpClient;

        public PatchService()
        {
            _httpClient = new HttpClient();
        }

        // 실제 서버 버전 정보 조회 후 반환
        public async Task<RemoteVersionInfo?> GetRemoteVersionAsync(string url)
        {
            string json = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<RemoteVersionInfo>(json, options);
        }

        // Manifest 조회
        public async Task<PatchManifest?> GetManifestAsync(string url)
        {
            string json = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<PatchManifest>(json, options);
        }

        // 파일 업데이트 비교
        public List<PatchFileInfo> GetFilesToUpdate(string gameDirectory, PatchManifest manifest)
        {
            var filesToUpdate = new List<PatchFileInfo>();

            foreach (var patchFile in manifest.Files)
            {
                string relativePath = patchFile.Path.Replace('/', Path.DirectorySeparatorChar);
                string localPath = Path.Combine(gameDirectory, relativePath);

                if (!File.Exists(localPath))
                {
                    filesToUpdate.Add(patchFile);
                    continue;
                }

                var fileInfo = new FileInfo(localPath);

                if (fileInfo.Length != patchFile.Size)
                {
                    filesToUpdate.Add(patchFile);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(patchFile.Hash))
                {
                    string localHash = CalculateFileHash(localPath);

                    if (!localHash.Equals(patchFile.Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        filesToUpdate.Add(patchFile);
                    }
                }
            }

            return filesToUpdate;
        }

        // 파일 다운로드
        public async Task DownloadFileAsync(
            string url,
            string destinationPath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            string? directory = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var inputStream = await response.Content.ReadAsStreamAsync();

            await using var outputStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                true);

            byte[] buffer = new byte[81920];

            long totalRead = 0;

            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(
                buffer,
                cancellationToken)) > 0)
            {
                await outputStream.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);

                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    double percentage =
                        (double)totalRead / totalBytes.Value * 100;

                    progress?.Report(percentage);
                }
            }
        }

        // SHA-256 해시 계산
        public string CalculateFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();

            byte[] hash = sha256.ComputeHash(stream);

            return Convert.ToHexString(hash);
        }

        // 해시 검증
        public bool VerifyFileHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(expectedHash))
                return true;

            if (!File.Exists(filePath))
                return false;

            string actualHash = CalculateFileHash(filePath);

            return actualHash.Equals(
                expectedHash,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}