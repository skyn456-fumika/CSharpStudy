namespace GameServerManager.App.Models;

public class ServerProcessExitedEventArgs : EventArgs
{
    public Guid ServerId { get; init; }

    public int ExitCode { get; init; }

    public DateTime ExitedAt { get; init; }
}