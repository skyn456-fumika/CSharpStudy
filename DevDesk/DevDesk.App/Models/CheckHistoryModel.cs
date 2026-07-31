namespace DevDesk.App.Models;

public class CheckHistoryModel
{
    public long Id { get; init; }
    public string CheckType { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public int? StatusCode { get; init; }
    public long ResponseTimeMs { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime CheckedAt { get; init; }

    public string StatusText => IsSuccess ? "성공" : "실패";
    public string StatusCodeText => StatusCode?.ToString() ?? "-";
    public string CheckedAtText => CheckedAt.ToString("yyyy-MM-dd HH:mm:ss");
}