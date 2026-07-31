namespace DevDesk.App.Models;

public class AppSettingsModel
{
    public string HttpUrl { get; set; } = "https://example.com";
    public string TcpHost { get; set; } = "localhost";
    public string TcpPort { get; set; } = "7189";
    public string MonitorIntervalSeconds { get; set; } = "30";
    public string LogFilePath { get; set; } = string.Empty;
    public string MaxLogLinesText { get; set; } = "1000";
    public bool EnableStatusNotifications { get; set; } = true;
}