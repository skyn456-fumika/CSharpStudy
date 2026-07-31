namespace DevDesk.App.Models;

public class TcpCheckResultModel
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool IsSuccess { get; init; }
    public long ResponseTimeMs { get; init; }
    public DateTime CheckedAt { get; init; }
    public string Message { get; init; } = string.Empty;

    public string StatusText => IsSuccess ? "연결 성공" : "연결 실패";
    public string TargetText => $"{Host}:{Port}";
    public string CheckedAtText => CheckedAt.ToString("yyyy-MM-dd HH:mm:ss");
}