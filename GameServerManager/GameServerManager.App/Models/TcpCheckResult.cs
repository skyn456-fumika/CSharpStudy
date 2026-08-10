namespace GameServerManager.App.Models;

public class TcpCheckResult
{
    public bool IsSuccess { get; init; }

    public long ResponseTimeMs { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime CheckedAt { get; init; } = DateTime.Now;
}