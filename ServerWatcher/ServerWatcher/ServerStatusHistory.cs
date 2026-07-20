namespace ServerWatcher;

public class ServerStatusHistory
{
    public DateTime ChangedAt { get; set; }

    public string PreviousStatus { get; set; } = "";

    public string CurrentStatus { get; set; } = "";

    public string Message =>
        $"{PreviousStatus} → {CurrentStatus}";
}