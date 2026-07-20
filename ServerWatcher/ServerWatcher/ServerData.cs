namespace ServerWatcher;

public class ServerData
{
    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string StatusText { get; set; } = "미확인";

    public int? StatusCode { get; set; }

    public long? ResponseTimeMs { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public List<ServerStatusHistoryData> Histories { get; set; }
        = new();
}