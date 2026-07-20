using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace LogViewer;

public class MainViewModel : ObservableObject
{
    private readonly LogParser logParser;
    private readonly List<LogEntry> allLogs = new();
    private string selectedLogLevel = "전체";
    private string searchText = "";

    public MainViewModel()
    {
        logParser = new LogParser();
        OpenFileCommand = new RelayCommand(OpenFile);

    }

    public ObservableCollection<LogEntry> Logs { get; }
        = new();

    public RelayCommand OpenFileCommand { get; }

    public string[] LogLevels { get; }
    = { "전체", "INFO", "WARN", "ERROR" };

    public int FilteredLogCount => Logs.Count;

    public int TotalLogCount => allLogs.Count;

    private void OpenFile()
    {
        OpenFileDialog dialog = new()
        {
            Title = "로그 파일 선택",
            Filter = "로그 파일 (*.log;*.txt)|*.log;*.txt|모든 파일 (*.*)|*.*"
        };

        bool? result = dialog.ShowDialog();

        if (result != true)
        {
            return;
        }

        string[] lines = File.ReadAllLines(dialog.FileName);

        allLogs.Clear();

        foreach (string line in lines)
        {
            LogEntry entry = logParser.Parse(line);
            allLogs.Add(entry);
        }

        ApplyFilter();

        OnPropertyChanged(nameof(TotalLogCount));
    }

    public string SelectedLogLevel
    {
        get => selectedLogLevel;

        set
        {
            if (SetProperty(ref selectedLogLevel, value))
            {
                ApplyFilter();
            }
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<LogEntry> filteredLogs = allLogs;

        if (SelectedLogLevel != "전체")
        {
            filteredLogs = filteredLogs.Where(
                log => log.LogLevel.Equals(
                    SelectedLogLevel,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredLogs = filteredLogs.Where(
                log =>
                    log.Message.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    log.OriginalText.Contains(
                        SearchText,
                        StringComparison.OrdinalIgnoreCase));
        }

        Logs.Clear();

        foreach (LogEntry log in filteredLogs)
        {
            Logs.Add(log);
        }

        OnPropertyChanged(nameof(FilteredLogCount));
    }

    public string SearchText
    {
        get => searchText;

        set
        {
            if (SetProperty(ref searchText, value))
            {
                ApplyFilter();
            }
        }
    }
}