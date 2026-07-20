namespace LogViewer;

public class LogParser
{
    public LogEntry Parse(string line)
    {
        string[] parts = line.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4)
        {
            return new LogEntry
            {
                Message = line,
                OriginalText = line
            };
        }

        return new LogEntry
        {
            Time = $"{parts[0]} {parts[1]}",
            LogLevel = parts[2],
            Message = string.Join(" ", parts.Skip(3)),
            OriginalText = line
        };
    }
}