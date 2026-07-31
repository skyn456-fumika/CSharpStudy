namespace DevDesk.App.Models;

public class ProcessInfoModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double MemoryMb { get; init; }
    public string StartTime { get; init; } = "-";
}