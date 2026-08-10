namespace GameServerManager.App.Models;

public class ServerLogEntry
{
    public Guid ServerId { get; init; }

    public string ServerName { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string Level { get; init; } = "INFO";

    public string Message { get; init; } = string.Empty;

    public string CreatedAtText =>
        CreatedAt.ToString("HH:mm:ss");
}