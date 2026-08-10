namespace GameServerManager.App.Models;

public class AppSettings
{
    public int HistoryRetentionDays { get; set; } = 30;

    public bool StartHealthMonitoringOnLaunch { get; set; } = true;

    public bool MinimizeToTray { get; set; } = true;
}