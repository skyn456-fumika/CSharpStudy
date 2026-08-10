namespace GameServerManager.App.Models;

public enum ServerStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Crashed,
    ConnectionFailed
}