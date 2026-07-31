namespace DevDesk.App.Models;

public class HttpCheckResultModel
{
    public string Url { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public int? StatusCode { get; init; }
    public long ResponseTimeMs { get; init; }
    public DateTime CheckedAt { get; init; }
    public string Message { get; init; } = string.Empty;

    public string StatusText => IsSuccess ? "정상" : "실패";
    public string StatusCodeText => StatusCode?.ToString() ?? "-";
    public string CheckedAtText => CheckedAt.ToString("yyyy-MM-dd HH:mm:ss");
}