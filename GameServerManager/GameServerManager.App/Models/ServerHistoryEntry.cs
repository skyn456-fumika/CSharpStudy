namespace GameServerManager.App.Models;

public class ServerHistoryEntry
{
    public long Id { get; set; }

    public Guid ServerId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string ResultText =>
        IsSuccess ? "성공" : "실패";

    public string CreatedAtText =>
        CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}