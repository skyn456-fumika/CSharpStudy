namespace GameServerManager.App.Models;

public class ServerResourceSnapshot
{
    public double CpuUsagePercent { get; init; }

    public long MemoryUsageBytes { get; init; }

    public TimeSpan Uptime { get; init; }
}