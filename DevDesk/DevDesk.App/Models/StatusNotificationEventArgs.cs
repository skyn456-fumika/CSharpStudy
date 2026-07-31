namespace DevDesk.App.Models;

public class StatusNotificationEventArgs : EventArgs
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsError { get; init; }
}