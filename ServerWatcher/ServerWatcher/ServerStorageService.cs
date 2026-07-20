using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace ServerWatcher;

public class ServerStorageService
{
    private readonly string filePath;

    public ServerStorageService()
    {
        string appDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string directoryPath =
            Path.Combine(
                appDataPath,
                "ServerWatcher");

        Directory.CreateDirectory(directoryPath);

        filePath =
            Path.Combine(
                directoryPath,
                "servers.json");
    }

    public async Task SaveAsync(
        IEnumerable<ServerInfo> servers)
    {
        List<ServerData> data = servers
            .Select(server => new ServerData
            {
                Name = server.Name,
                Url = server.Url,
                StatusText = server.StatusText,
                StatusCode = server.StatusCode,
                ResponseTimeMs = server.ResponseTimeMs,
                LastCheckedAt = server.LastCheckedAt,

                Histories = server.Histories
                    .Select(history =>
                        new ServerStatusHistoryData
                        {
                            ChangedAt = history.ChangedAt,
                            PreviousStatus =
                                history.PreviousStatus,
                            CurrentStatus =
                                history.CurrentStatus
                        })
                    .ToList()
            })
            .ToList();

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        string json =
            JsonSerializer.Serialize(
                data,
                options);

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                json);
        }
        catch (IOException ex)
        {
            Debug.WriteLine(
                $"서버 데이터 파일 저장 실패: {ex.Message}");
        }
    }

    public async Task<List<ServerInfo>> LoadAsync()
    {
        if (!File.Exists(filePath))
        {
            return new List<ServerInfo>();
        }

        try
        {
            string json =
                await File.ReadAllTextAsync(
                    filePath);

            List<ServerData> data =
                JsonSerializer.Deserialize<List<ServerData>>(
                    json)
                ?? new List<ServerData>();

            return data
                .Select(CreateServerInfo)
                .ToList();
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(
                $"서버 데이터 JSON 해석 실패: {ex.Message}");

            return new List<ServerInfo>();
        }
        catch (IOException ex)
        {
            Debug.WriteLine(
                $"서버 데이터 파일 읽기 실패: {ex.Message}");

            return new List<ServerInfo>();
        }
    }

    private static ServerInfo CreateServerInfo(ServerData data)
    {
        ServerInfo server = new()
        {
            Name = data.Name,
            Url = data.Url,
            StatusText = string.IsNullOrWhiteSpace(data.StatusText)
                ? "미확인"
                : data.StatusText,
            StatusCode = data.StatusCode,
            ResponseTimeMs = data.ResponseTimeMs,
            LastCheckedAt = data.LastCheckedAt
        };

        foreach (ServerStatusHistoryData history
                 in data.Histories ?? new List<ServerStatusHistoryData>())
        {
            server.Histories.Add(
                new ServerStatusHistory
                {
                    ChangedAt = history.ChangedAt,
                    PreviousStatus = history.PreviousStatus,
                    CurrentStatus = history.CurrentStatus
                });
        }

        return server;
    }
}