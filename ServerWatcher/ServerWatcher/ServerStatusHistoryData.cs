namespace ServerWatcher;

public class ServerStatusHistoryData
{
    public DateTime ChangedAt { get; set; }

    public string PreviousStatus { get; set; } = "";

    public string CurrentStatus { get; set; } = "";
}