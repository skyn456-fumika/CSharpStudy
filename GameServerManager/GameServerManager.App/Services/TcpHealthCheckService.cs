using System.Diagnostics;
using System.Net.Sockets;
using GameServerManager.App.Models;

namespace GameServerManager.App.Services;

public class TcpHealthCheckService
{
    public async Task<TcpCheckResult> CheckAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutSource.CancelAfter(timeout);

            await client.ConnectAsync(
                host,
                port,
                timeoutSource.Token);

            stopwatch.Stop();

            return new TcpCheckResult
            {
                IsSuccess = true,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = "TCP 연결에 성공했습니다.",
                CheckedAt = DateTime.Now
            };
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            return new TcpCheckResult
            {
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = "TCP 연결 시간이 초과되었습니다.",
                CheckedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new TcpCheckResult
            {
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message,
                CheckedAt = DateTime.Now
            };
        }
    }
}