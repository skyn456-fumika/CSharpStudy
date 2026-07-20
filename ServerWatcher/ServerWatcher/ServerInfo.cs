using System.Collections.ObjectModel;

namespace ServerWatcher;

public class ServerInfo : ObservableObject
{
    private string name = "";
    private string url = "";
    private string statusText = "미확인";
    private int? statusCode;
    private long? responseTimeMs;
    private DateTime? lastCheckedAt;
    private bool isChecking;
    private string errorMessage = "";

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string Url
    {
        get => url;
        set => SetProperty(ref url, value);
    }

    public string StatusText
    {
        get => statusText;
        set => SetProperty(ref statusText, value);
    }

    public int? StatusCode
    {
        get => statusCode;
        set => SetProperty(ref statusCode, value);
    }

    public long? ResponseTimeMs
    {
        get => responseTimeMs;
        set => SetProperty(ref responseTimeMs, value);
    }

    public DateTime? LastCheckedAt
    {
        get => lastCheckedAt;
        set => SetProperty(ref lastCheckedAt, value);
    }

    public bool IsChecking
    {
        get => isChecking;
        set => SetProperty(ref isChecking, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        set => SetProperty(ref errorMessage, value);
    }

    public ObservableCollection<ServerStatusHistory> Histories { get; }
        = new();
}