namespace DevDesk.App.Models;

public class LogEntryModel
{
    public DateTime DetectedAt { get; init; }
    public string Content { get; init; } = string.Empty;

    public string DetectedAtText =>
        DetectedAt.ToString("HH:mm:ss");
}