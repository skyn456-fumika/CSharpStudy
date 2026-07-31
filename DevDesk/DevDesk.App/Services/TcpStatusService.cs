using System.Diagnostics;
using System.Net.Sockets;
using DevDesk.App.Models;

namespace DevDesk.App.Services;

public class TcpStatusService
{
    public async Task<TcpCheckResultModel> CheckAsync(
        string host,
        int port,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("호스트를 입력하세요.");
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentException("포트는 1부터 65535 사이여야 합니다.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutSource.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(
                host.Trim(),
                port,
                timeoutSource.Token);

            stopwatch.Stop();

            return new TcpCheckResultModel
            {
                Host = host.Trim(),
                Port = port,
                IsSuccess = true,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.Now,
                Message = "TCP 포트 연결에 성공했습니다."
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            return new TcpCheckResultModel
            {
                Host = host.Trim(),
                Port = port,
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.Now,
                Message = "TCP 연결 제한 시간 5초를 초과했습니다."
            };
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();

            return new TcpCheckResultModel
            {
                Host = host.Trim(),
                Port = port,
                IsSuccess = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.Now,
                Message = $"TCP 연결 실패: {ex.Message}"
            };
        }
    }
}