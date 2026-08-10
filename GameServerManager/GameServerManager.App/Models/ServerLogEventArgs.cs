namespace GameServerManager.App.Models;

public class ServerLogEventArgs : EventArgs
{
    public Guid ServerId { get; init; }

    public string ServerName { get; init; } = string.Empty;

    public string Level { get; init; } = "INFO";

    public string Message { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.Now;
}