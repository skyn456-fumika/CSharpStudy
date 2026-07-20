using System.Diagnostics;
using System.Net.Http;

namespace ServerWatcher;

public class ServerCheckService
{
    private readonly HttpClient httpClient;

    public ServerCheckService()
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task CheckAsync(ServerInfo server)
    {
        string previousStatus = server.StatusText;

        server.IsChecking = true;
        server.StatusText = "확인 중...";
        server.StatusCode = null;
        server.ResponseTimeMs = null;
        server.ErrorMessage = "";

        Stopwatch stopwatch = Stopwatch.StartNew();

        string finalStatus;

        try
        {
            using HttpResponseMessage response =
                await httpClient.GetAsync(server.Url);

            stopwatch.Stop();

            server.StatusCode = (int)response.StatusCode;

            server.ResponseTimeMs =
                stopwatch.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                finalStatus = "정상";
                server.ErrorMessage = "";
            }
            else
            {
                finalStatus = "HTTP 오류";
                server.ErrorMessage =
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();

            server.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            server.ErrorMessage = "요청 시간이 초과되었습니다.";

            finalStatus = "시간 초과";
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();

            server.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            server.ErrorMessage = ex.Message;

            finalStatus = "연결 실패";
        }
        finally
        {
            server.LastCheckedAt = DateTime.Now;
            server.IsChecking = false;
        }

        server.StatusText = finalStatus;

        AddHistoryIfChanged(
            server,
            previousStatus,
            finalStatus);
    }

    private static void AddHistoryIfChanged(
        ServerInfo server,
        string previousStatus,
        string currentStatus)
    {
        if (previousStatus == currentStatus)
        {
            return;
        }

        server.Histories.Insert(
            0,
            new ServerStatusHistory
            {
                ChangedAt = DateTime.Now,
                PreviousStatus = previousStatus,
                CurrentStatus = currentStatus
            });

        const int maximumHistoryCount = 100;

        while (server.Histories.Count >
               maximumHistoryCount)
        {
            server.Histories.RemoveAt(
                server.Histories.Count - 1);
        }
    }
}