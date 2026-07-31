using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using DevDesk.App.Commands;
using DevDesk.App.Models;
using DevDesk.App.Services;
using DevDesk.App.Data;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DevDesk.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ProcessService _processService = new();
    private readonly HttpStatusService _httpStatusService = new();
    private readonly TcpStatusService _tcpStatusService = new();
    private readonly ServerMonitorService _serverMonitorService = new();
    private readonly LogFileWatcherService _logFileWatcherService = new();
    private readonly DevDeskDatabase _database = new();
    private readonly SettingsService _settingsService = new();

    private string _currentPageTitle = "대시보드";
    private string _statusMessage = "DevDesk가 준비되었습니다.";
    private string _processFilePath = string.Empty;
    private string _processSearchText = string.Empty;
    private ProcessInfoModel? _selectedProcess;

    private string _httpUrl = "https://example.com";
    private HttpCheckResultModel? _httpCheckResult;
    private string _tcpHost = "localhost";
    private string _tcpPort = "7189";
    private TcpCheckResultModel? _tcpCheckResult;

    private string _monitorIntervalSeconds = "30";
    private bool _isMonitoring;

    private string _logFilePath = string.Empty;
    private bool _isLogWatching;

    private string _logSearchText = string.Empty;
    private string _maxLogLinesText = "1000";

    private string _historyTypeFilter = "전체";
    private CheckHistoryModel? _selectedHistory;

    private bool _isSettingsLoaded;

    private bool? _previousHttpSuccess;
    private bool? _previousTcpSuccess;

    private int _dashboardProcessCount;

    private bool _enableStatusNotifications = true;

    public event EventHandler<StatusNotificationEventArgs>? StatusNotificationRequested;

    public MainViewModel()
    {
        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = FilterProcess;

        LogEntriesView = CollectionViewSource.GetDefaultView(LogEntries);
        LogEntriesView.Filter = FilterLogEntry;

        CheckHistoriesView =
            CollectionViewSource.GetDefaultView(CheckHistories);
        CheckHistoriesView.Filter = FilterCheckHistory;

        ShowDashboardCommand =
            new AsyncRelayCommand(ShowDashboardAsync);
        ShowProcessesCommand = new RelayCommand(ShowProcesses);
        ShowServerMonitorCommand = new RelayCommand(ShowServerMonitor);
        ShowLogMonitorCommand = new RelayCommand(() => ChangePage("로그 감시"));
        ShowHistoryCommand = new AsyncRelayCommand(ShowHistoryAsync);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ShowLogMonitorCommand =
            new RelayCommand(() => ChangePage("로그 감시"));

        RefreshProcessesCommand = new RelayCommand(LoadProcesses);
        BrowseProcessCommand = new RelayCommand(BrowseProcessFile);
        StartProcessCommand = new RelayCommand(StartProcess, CanStartProcess);
        KillProcessCommand = new RelayCommand(
            KillSelectedProcess,
            CanKillSelectedProcess);
        ClearProcessSearchCommand = new RelayCommand(
            ClearProcessSearch,
            CanClearProcessSearch);
        CheckHttpCommand = new AsyncRelayCommand(
            CheckHttpAsync,
            CanCheckHttp);
        CheckTcpCommand = new AsyncRelayCommand(
            CheckTcpAsync,
            CanCheckTcp);
        StartMonitoringCommand = new RelayCommand(
            StartMonitoring,
            CanStartMonitoring);
        StopMonitoringCommand = new AsyncRelayCommand(
            StopMonitoringAsync,
            () => IsMonitoring);
        ShowLogMonitorCommand = new RelayCommand(ShowLogMonitor);

        BrowseLogFileCommand = new RelayCommand(BrowseLogFile);

        StartLogWatchingCommand = new AsyncRelayCommand(
            StartLogWatchingAsync,
            CanStartLogWatching);

        StopLogWatchingCommand = new AsyncRelayCommand(
            StopLogWatchingAsync,
            () => IsLogWatching);

        ClearLogsCommand = new RelayCommand(
            ClearLogs,
            () => LogEntries.Count > 0);

        ClearLogSearchCommand = new RelayCommand(
            ClearLogSearch,
            () => !string.IsNullOrWhiteSpace(LogSearchText));

        RefreshHistoriesCommand =
            new AsyncRelayCommand(LoadHistoriesAsync);

        DeleteAllHistoriesCommand =
            new AsyncRelayCommand(
                DeleteAllHistoriesAsync,
                () => CheckHistories.Count > 0);

        SaveSettingsCommand =
            new AsyncRelayCommand(SaveSettingsAsync);

        _logFileWatcherService.LineAdded += OnLogLineAdded;
        _logFileWatcherService.WatcherError += OnLogWatcherError;
        _ = InitializeAsync();

    }

    public ObservableCollection<ProcessInfoModel> Processes { get; } = [];

    public ICollectionView ProcessesView { get; }

    public ObservableCollection<LogEntryModel> LogEntries { get; } = [];

    public ICollectionView LogEntriesView { get; }

    public ObservableCollection<CheckHistoryModel> CheckHistories { get; } = [];

    public ICollectionView CheckHistoriesView { get; }

    public ObservableCollection<CheckHistoryModel> RecentHistories { get; } = [];

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        set => SetProperty(ref _currentPageTitle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ProcessFilePath
    {
        get => _processFilePath;
        set
        {
            if (SetProperty(ref _processFilePath, value))
            {
                StartProcessCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ProcessSearchText
    {
        get => _processSearchText;
        set
        {
            if (SetProperty(ref _processSearchText, value))
            {
                ProcessesView.Refresh();
                ClearProcessSearchCommand.RaiseCanExecuteChanged();
                UpdateProcessFilterStatus();
            }
        }
    }

    public ProcessInfoModel? SelectedProcess
    {
        get => _selectedProcess;
        set
        {
            if (SetProperty(ref _selectedProcess, value))
            {
                KillProcessCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string HttpUrl
    {
        get => _httpUrl;
        set
        {
            if (SetProperty(ref _httpUrl, value))
            {
                CheckHttpCommand.RaiseCanExecuteChanged();
                StartMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public HttpCheckResultModel? HttpCheckResult
    {
        get => _httpCheckResult;
        set
        {
            if (SetProperty(ref _httpCheckResult, value))
            {
                OnPropertyChanged(nameof(DashboardHttpStatus));
            }
        }
    }

    public string TcpHost
    {
        get => _tcpHost;
        set
        {
            if (SetProperty(ref _tcpHost, value))
            {
                CheckTcpCommand.RaiseCanExecuteChanged();
                StartMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string TcpPort
    {
        get => _tcpPort;
        set
        {
            if (SetProperty(ref _tcpPort, value))
            {
                CheckTcpCommand.RaiseCanExecuteChanged();
                StartMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public TcpCheckResultModel? TcpCheckResult
    {
        get => _tcpCheckResult;
        set
        {
            if (SetProperty(ref _tcpCheckResult, value))
            {
                OnPropertyChanged(nameof(DashboardTcpStatus));
            }
        }
    }

    public string MonitorIntervalSeconds
    {
        get => _monitorIntervalSeconds;
        set
        {
            if (SetProperty(ref _monitorIntervalSeconds, value))
            {
                StartMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                OnPropertyChanged(nameof(MonitoringStatusText));
                OnPropertyChanged(nameof(DashboardMonitoringStatus));

                StartMonitoringCommand.RaiseCanExecuteChanged();
                StopMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string MonitoringStatusText =>
        IsMonitoring ? "감시 중" : "감시 중지";

    public string LogFilePath
    {
        get => _logFilePath;
        set
        {
            if (SetProperty(ref _logFilePath, value))
            {
                StartLogWatchingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLogWatching
    {
        get => _isLogWatching;
        private set
        {
            if (SetProperty(ref _isLogWatching, value))
            {
                OnPropertyChanged(nameof(LogWatchingStatusText));
                OnPropertyChanged(nameof(DashboardLogStatus));

                StartLogWatchingCommand.RaiseCanExecuteChanged();
                StopLogWatchingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LogWatchingStatusText =>
        IsLogWatching ? "로그 감시 중" : "감시 중지";

    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            if (SetProperty(ref _logSearchText, value))
            {
                LogEntriesView.Refresh();
                ClearLogSearchCommand.RaiseCanExecuteChanged();
                UpdateLogStatus();
            }
        }
    }

    public string MaxLogLinesText
    {
        get => _maxLogLinesText;
        set => SetProperty(ref _maxLogLinesText, value);
    }

    public IReadOnlyList<string> HistoryTypeFilters { get; } =
[
    "전체",
    "HTTP",
    "TCP"
];

    public string HistoryTypeFilter
    {
        get => _historyTypeFilter;
        set
        {
            if (SetProperty(ref _historyTypeFilter, value))
            {
                CheckHistoriesView.Refresh();
                UpdateHistoryStatus();
            }
        }
    }

    public CheckHistoryModel? SelectedHistory
    {
        get => _selectedHistory;
        set => SetProperty(ref _selectedHistory, value);
    }

    public int DashboardProcessCount
    {
        get => _dashboardProcessCount;
        private set => SetProperty(ref _dashboardProcessCount, value);
    }

    public string DashboardHttpStatus =>
        HttpCheckResult?.StatusText ?? "검사 전";

    public string DashboardTcpStatus =>
        TcpCheckResult?.StatusText ?? "검사 전";

    public string DashboardMonitoringStatus =>
        IsMonitoring ? "감시 중" : "감시 중지";

    public string DashboardLogStatus =>
        IsLogWatching ? "감시 중" : "감시 중지";

    public bool EnableStatusNotifications
    {
        get => _enableStatusNotifications;
        set => SetProperty(ref _enableStatusNotifications, value);
    }

    public AsyncRelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowProcessesCommand { get; }
    public RelayCommand ShowServerMonitorCommand { get; }
    public RelayCommand ShowLogMonitorCommand { get; }
    public AsyncRelayCommand ShowHistoryCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }

    public RelayCommand RefreshProcessesCommand { get; }
    public RelayCommand BrowseProcessCommand { get; }
    public RelayCommand StartProcessCommand { get; }
    public RelayCommand KillProcessCommand { get; }
    public RelayCommand ClearProcessSearchCommand { get; }

    public AsyncRelayCommand CheckHttpCommand { get; }

    public AsyncRelayCommand CheckTcpCommand { get; }

    public RelayCommand StartMonitoringCommand { get; }
    public AsyncRelayCommand StopMonitoringCommand { get; }

    public RelayCommand BrowseLogFileCommand { get; }
    public AsyncRelayCommand StartLogWatchingCommand { get; }
    public AsyncRelayCommand StopLogWatchingCommand { get; }
    public RelayCommand ClearLogsCommand { get; }

    public RelayCommand ClearLogSearchCommand { get; }

    public AsyncRelayCommand RefreshHistoriesCommand { get; }
    public AsyncRelayCommand DeleteAllHistoriesCommand { get; }

    public AsyncRelayCommand SaveSettingsCommand { get; }

    private void ShowProcesses()
    {
        ChangePage("프로세스 관리");
        LoadProcesses();
    }

    private void LoadProcesses()
    {
        try
        {
            var processList = _processService.GetProcesses();

            Processes.Clear();

            foreach (var process in processList)
            {
                Processes.Add(process);
            }

            SelectedProcess = null;
            ProcessesView.Refresh();
            UpdateProcessFilterStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"프로세스 조회 실패: {ex.Message}";
        }
    }

    private bool FilterProcess(object item)
    {
        if (item is not ProcessInfoModel process)
        {
            return false;
        }

        var searchText = ProcessSearchText.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return process.Name.Contains(
                   searchText,
                   StringComparison.OrdinalIgnoreCase)
               || process.Id.ToString().Contains(searchText);
    }

    private void UpdateProcessFilterStatus()
    {
        var visibleCount = ProcessesView.Cast<object>().Count();

        if (string.IsNullOrWhiteSpace(ProcessSearchText))
        {
            StatusMessage = $"프로세스 {Processes.Count}개를 불러왔습니다.";
            return;
        }

        StatusMessage =
            $"전체 {Processes.Count}개 중 {visibleCount}개가 검색되었습니다.";
    }

    private bool CanClearProcessSearch()
    {
        return !string.IsNullOrWhiteSpace(ProcessSearchText);
    }

    private void ClearProcessSearch()
    {
        ProcessSearchText = string.Empty;
    }

    private void BrowseProcessFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "실행할 프로그램 선택",
            Filter = "실행 파일 (*.exe)|*.exe|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ProcessFilePath = dialog.FileName;
            StatusMessage = "실행할 프로그램을 선택했습니다.";
        }
    }

    private bool CanStartProcess()
    {
        return !string.IsNullOrWhiteSpace(ProcessFilePath);
    }

    private void StartProcess()
    {
        var filePath = ProcessFilePath.Trim();

        if (!File.Exists(filePath))
        {
            StatusMessage = "실행 파일을 찾을 수 없습니다.";
            return;
        }

        try
        {
            _processService.StartProcess(filePath);
            StatusMessage = $"{Path.GetFileName(filePath)} 프로그램을 실행했습니다.";

            ProcessFilePath = string.Empty;
            LoadProcesses();
        }
        catch (Exception ex)
        {
            StatusMessage = $"프로그램 실행 실패: {ex.Message}";
        }
    }

    private bool CanKillSelectedProcess()
    {
        return SelectedProcess is not null;
    }

    private void KillSelectedProcess()
    {
        if (SelectedProcess is null)
        {
            return;
        }

        var selectedProcess = SelectedProcess;

        if (selectedProcess.Id == Environment.ProcessId)
        {
            MessageBox.Show(
                "DevDesk 자신의 프로세스는 종료할 수 없습니다.",
                "프로세스 종료",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var result = MessageBox.Show(
            $"{selectedProcess.Name} 프로세스를 종료하시겠습니까?\n\nPID: {selectedProcess.Id}",
            "프로세스 종료 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _processService.KillProcess(selectedProcess.Id);
            StatusMessage = $"{selectedProcess.Name} 프로세스를 종료했습니다.";
            LoadProcesses();
        }
        catch (ArgumentException)
        {
            StatusMessage = "이미 종료된 프로세스입니다.";
            LoadProcesses();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            StatusMessage = "프로세스를 종료할 권한이 없습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"프로세스 종료 실패: {ex.Message}";
        }
    }

    private void ChangePage(string pageTitle)
    {
        CurrentPageTitle = pageTitle;
        StatusMessage = $"{pageTitle} 화면을 선택했습니다.";
    }

    private void ShowServerMonitor()
    {
        ChangePage("서버 상태 검사");
    }

    private bool CanCheckHttp()
    {
        return !string.IsNullOrWhiteSpace(HttpUrl);
    }

    private async Task CheckHttpAsync()
    {
        var url = HttpUrl.Trim();

        StatusMessage = $"{url} 서버 상태를 확인하고 있습니다.";

        try
        {
            HttpCheckResult = await _httpStatusService.CheckAsync(url);
            await SaveHttpHistoryAsync(HttpCheckResult);

            CheckHttpStatusChange(HttpCheckResult);

            StatusMessage = HttpCheckResult.IsSuccess
                ? $"HTTP 검사 성공: {HttpCheckResult.StatusCodeText}, " +
                  $"{HttpCheckResult.ResponseTimeMs}ms"
                : $"HTTP 검사 실패: {HttpCheckResult.Message}";
        }
        catch (ArgumentException ex)
        {
            HttpCheckResult = null;
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            HttpCheckResult = null;
            StatusMessage = $"HTTP 검사 중 오류가 발생했습니다: {ex.Message}";
        }
    }

    private bool CanCheckTcp()
    {
        return !string.IsNullOrWhiteSpace(TcpHost)
               && !string.IsNullOrWhiteSpace(TcpPort);
    }

    private async Task CheckTcpAsync()
    {
        var host = TcpHost.Trim();

        if (!int.TryParse(TcpPort.Trim(), out var port))
        {
            TcpCheckResult = null;
            StatusMessage = "포트는 숫자로 입력하세요.";
            return;
        }

        StatusMessage = $"{host}:{port} TCP 연결을 확인하고 있습니다.";

        try
        {
            TcpCheckResult = await _tcpStatusService.CheckAsync(host, port);
            await SaveTcpHistoryAsync(TcpCheckResult);

            CheckTcpStatusChange(TcpCheckResult);

            StatusMessage = TcpCheckResult.IsSuccess
                ? $"TCP 연결 성공: {TcpCheckResult.TargetText}, " +
                  $"{TcpCheckResult.ResponseTimeMs}ms"
                : TcpCheckResult.Message;
        }
        catch (ArgumentException ex)
        {
            TcpCheckResult = null;
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            TcpCheckResult = null;
            StatusMessage = $"TCP 검사 중 오류가 발생했습니다: {ex.Message}";
        }
    }

    private bool CanStartMonitoring()
    {
        return !IsMonitoring
               && !string.IsNullOrWhiteSpace(HttpUrl)
               && !string.IsNullOrWhiteSpace(TcpHost)
               && !string.IsNullOrWhiteSpace(TcpPort)
               && !string.IsNullOrWhiteSpace(MonitorIntervalSeconds);
    }

    private void StartMonitoring()
    {
        if (!TryGetMonitoringValues(
                out var intervalSeconds,
                out _,
                out var errorMessage))
        {
            StatusMessage = errorMessage;
            return;
        }

        try
        {
            _serverMonitorService.Start(
                intervalSeconds,
                MonitorTargetsAsync);

            IsMonitoring = true;
            StatusMessage =
                $"{intervalSeconds}초 간격으로 서버 감시를 시작했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"서버 감시 시작 실패: {ex.Message}";
        }
    }

    private async Task StopMonitoringAsync()
    {
        await _serverMonitorService.StopAsync();

        IsMonitoring = false;
        StatusMessage = "서버 감시를 중지했습니다.";
    }

    private async Task MonitorTargetsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetMonitoringValues(
                    out _,
                    out var port,
                    out var errorMessage))
            {
                await UpdateMonitoringErrorAsync(errorMessage);
                return;
            }

            var url = HttpUrl.Trim();
            var host = TcpHost.Trim();

            var httpTask = _httpStatusService.CheckAsync(
                url,
                cancellationToken);

            var tcpTask = _tcpStatusService.CheckAsync(
                host,
                port,
                cancellationToken);

            await Task.WhenAll(httpTask, tcpTask);

            var httpResult = await httpTask;
            var tcpResult = await tcpTask;

            await SaveHttpHistoryAsync(httpResult);
            await SaveTcpHistoryAsync(tcpResult);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                HttpCheckResult = httpResult;
                TcpCheckResult = tcpResult;

                CheckHttpStatusChange(httpResult);
                CheckTcpStatusChange(tcpResult);

                var httpStatus = httpResult.IsSuccess ? "HTTP 정상" : "HTTP 실패";
                var tcpStatus = tcpResult.IsSuccess ? "TCP 정상" : "TCP 실패";

                StatusMessage =
                    $"자동 감시 완료: {httpStatus}, {tcpStatus} " +
                    $"({DateTime.Now:HH:mm:ss})";
            });
        }
        catch (OperationCanceledException)
        {
            // 사용자가 감시를 중지한 경우
        }
        catch (Exception ex)
        {
            await UpdateMonitoringErrorAsync(
                $"자동 감시 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    private bool TryGetMonitoringValues(
        out int intervalSeconds,
        out int port,
        out string errorMessage)
    {
        intervalSeconds = 0;
        port = 0;
        errorMessage = string.Empty;

        if (!Uri.TryCreate(HttpUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage =
                "HTTP 주소는 http:// 또는 https://로 시작해야 합니다.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(TcpHost))
        {
            errorMessage = "TCP 호스트를 입력하세요.";
            return false;
        }

        if (!int.TryParse(TcpPort.Trim(), out port))
        {
            errorMessage = "TCP 포트는 숫자로 입력하세요.";
            return false;
        }

        if (port is < 1 or > 65535)
        {
            errorMessage = "TCP 포트는 1부터 65535 사이여야 합니다.";
            return false;
        }

        if (!int.TryParse(
                MonitorIntervalSeconds.Trim(),
                out intervalSeconds))
        {
            errorMessage = "감시 주기는 숫자로 입력하세요.";
            return false;
        }

        if (intervalSeconds < 1)
        {
            errorMessage = "감시 주기는 1초 이상이어야 합니다.";
            return false;
        }

        return true;
    }

    private async Task UpdateMonitoringErrorAsync(string message)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = message;
        });
    }

    private void ShowLogMonitor()
    {
        ChangePage("로그 감시");
    }

    private void BrowseLogFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "감시할 로그 파일 선택",
            Filter = "로그 및 텍스트 파일 (*.log;*.txt)|*.log;*.txt|" +
                     "모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            LogFilePath = dialog.FileName;
            StatusMessage = "감시할 로그 파일을 선택했습니다.";
        }
    }

    private bool CanStartLogWatching()
    {
        return !IsLogWatching
               && !string.IsNullOrWhiteSpace(LogFilePath);
    }

    private async Task StartLogWatchingAsync()
    {
        var filePath = LogFilePath.Trim();

        if (!File.Exists(filePath))
        {
            StatusMessage = "감시할 로그 파일을 찾을 수 없습니다.";
            return;
        }

        try
        {
            var existingLines =
                await _logFileWatcherService.StartAsync(filePath);

            LogEntries.Clear();

            var maxLines = GetMaxLogLines();
            var linesToDisplay = existingLines.TakeLast(maxLines);

            foreach (var line in linesToDisplay)
            {
                LogEntries.Add(new LogEntryModel
                {
                    DetectedAt = DateTime.Now,
                    Content = line
                });
            }

            LogEntriesView.Refresh();

            IsLogWatching = true;
            ClearLogsCommand.RaiseCanExecuteChanged();

            StatusMessage =
                $"로그 감시를 시작했습니다. 기존 로그 {LogEntries.Count}줄을 불러왔습니다.";
        }
        catch (Exception ex)
        {
            IsLogWatching = false;
            StatusMessage = $"로그 감시 시작 실패: {ex.Message}";
        }
    }

    private async Task StopLogWatchingAsync()
    {
        await _logFileWatcherService.StopAsync();

        IsLogWatching = false;
        StatusMessage = "로그 감시를 중지했습니다.";
    }

    private void ClearLogs()
    {
        LogEntries.Clear();
        LogEntriesView.Refresh();

        ClearLogsCommand.RaiseCanExecuteChanged();
        StatusMessage = "화면의 로그 목록을 초기화했습니다.";
    }

    private void OnLogLineAdded(object? sender, string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(new LogEntryModel
            {
                DetectedAt = DateTime.Now,
                Content = line
            });

            TrimLogEntries();
            LogEntriesView.Refresh();

            ClearLogsCommand.RaiseCanExecuteChanged();
            UpdateLogStatus();
        });
    }

    private void OnLogWatcherError(object? sender, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsLogWatching = false;
            StatusMessage = message;
        });
    }

    private bool FilterLogEntry(object item)
    {
        if (item is not LogEntryModel logEntry)
        {
            return false;
        }

        var searchText = LogSearchText.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return logEntry.Content.Contains(
            searchText,
            StringComparison.OrdinalIgnoreCase);
    }

    private void ClearLogSearch()
    {
        LogSearchText = string.Empty;
    }

    private void UpdateLogStatus()
    {
        var visibleCount = LogEntriesView.Cast<object>().Count();

        if (string.IsNullOrWhiteSpace(LogSearchText))
        {
            StatusMessage = $"현재 화면에 로그 {LogEntries.Count}줄이 있습니다.";
            return;
        }

        StatusMessage =
            $"전체 {LogEntries.Count}줄 중 {visibleCount}줄이 검색되었습니다.";
    }

    private int GetMaxLogLines()
    {
        if (!int.TryParse(MaxLogLinesText.Trim(), out var maxLines) ||
            maxLines < 1)
        {
            return 1000;
        }

        return maxLines;
    }

    private void TrimLogEntries()
    {
        var maxLines = GetMaxLogLines();

        while (LogEntries.Count > maxLines)
        {
            LogEntries.RemoveAt(0);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _database.InitializeAsync();
            await LoadSettingsAsync();
            await LoadDashboardAsync();

            StatusMessage = "DevDesk가 준비되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"초기화 실패: {ex.Message}";
        }
    }

    private async Task SaveHttpHistoryAsync(HttpCheckResultModel result)
    {
        await _database.AddHistoryAsync(new CheckHistoryModel
        {
            CheckType = "HTTP",
            Target = result.Url,
            IsSuccess = result.IsSuccess,
            StatusCode = result.StatusCode,
            ResponseTimeMs = result.ResponseTimeMs,
            Message = result.Message,
            CheckedAt = result.CheckedAt
        });

        await RefreshRecentHistoriesAsync();
    }

    private async Task SaveTcpHistoryAsync(TcpCheckResultModel result)
    {
        await _database.AddHistoryAsync(new CheckHistoryModel
        {
            CheckType = "TCP",
            Target = result.TargetText,
            IsSuccess = result.IsSuccess,
            StatusCode = null,
            ResponseTimeMs = result.ResponseTimeMs,
            Message = result.Message,
            CheckedAt = result.CheckedAt
        });

        await RefreshRecentHistoriesAsync();
    }

    private async Task RefreshRecentHistoriesAsync()
    {
        var histories =
            await _database.GetHistoriesAsync(5);

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RecentHistories.Clear();

            foreach (var history in histories)
            {
                RecentHistories.Add(history);
            }
        });
    }

    private async Task ShowHistoryAsync()
    {
        ChangePage("이력 조회");
        await LoadHistoriesAsync();
    }

    private async Task LoadHistoriesAsync()
    {
        try
        {
            var histories = await _database.GetHistoriesAsync();

            CheckHistories.Clear();

            foreach (var history in histories)
            {
                CheckHistories.Add(history);
            }

            SelectedHistory = null;
            CheckHistoriesView.Refresh();
            DeleteAllHistoriesCommand.RaiseCanExecuteChanged();

            UpdateHistoryStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"이력 조회 실패: {ex.Message}";
        }
    }

    private bool FilterCheckHistory(object item)
    {
        if (item is not CheckHistoryModel history)
        {
            return false;
        }

        if (HistoryTypeFilter == "전체")
        {
            return true;
        }

        return string.Equals(
            history.CheckType,
            HistoryTypeFilter,
            StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateHistoryStatus()
    {
        var visibleCount = CheckHistoriesView.Cast<object>().Count();

        if (HistoryTypeFilter == "전체")
        {
            StatusMessage = $"검사 이력 {CheckHistories.Count}건을 불러왔습니다.";
            return;
        }

        StatusMessage =
            $"전체 {CheckHistories.Count}건 중 " +
            $"{HistoryTypeFilter} 이력 {visibleCount}건이 표시됩니다.";
    }

    private async Task DeleteAllHistoriesAsync()
    {
        var result = MessageBox.Show(
            "저장된 HTTP·TCP 검사 이력을 모두 삭제하시겠습니까?\n\n" +
            "삭제한 이력은 복구할 수 없습니다.",
            "검사 이력 전체 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _database.DeleteAllHistoriesAsync();

            CheckHistories.Clear();
            SelectedHistory = null;
            CheckHistoriesView.Refresh();
            DeleteAllHistoriesCommand.RaiseCanExecuteChanged();

            StatusMessage = "저장된 검사 이력을 모두 삭제했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"검사 이력 삭제 실패: {ex.Message}";
        }
    }

    private void ShowSettings()
    {
        ChangePage("설정");
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();

        HttpUrl = settings.HttpUrl;
        TcpHost = settings.TcpHost;
        TcpPort = settings.TcpPort;
        MonitorIntervalSeconds = settings.MonitorIntervalSeconds;
        LogFilePath = settings.LogFilePath;
        MaxLogLinesText = settings.MaxLogLinesText;
        EnableStatusNotifications = settings.EnableStatusNotifications;

        _isSettingsLoaded = true;
    }

    private async Task SaveSettingsAsync()
    {
        if (!_isSettingsLoaded)
        {
            StatusMessage = "설정 초기화가 아직 완료되지 않았습니다.";
            return;
        }

        var validationMessage = ValidateSettings();

        if (validationMessage is not null)
        {
            StatusMessage = validationMessage;
            return;
        }

        try
        {
            var settings = new AppSettingsModel
            {
                HttpUrl = HttpUrl.Trim(),
                TcpHost = TcpHost.Trim(),
                TcpPort = TcpPort.Trim(),
                MonitorIntervalSeconds =
                    MonitorIntervalSeconds.Trim(),
                LogFilePath = LogFilePath.Trim(),
                MaxLogLinesText = MaxLogLinesText.Trim(),
                EnableStatusNotifications = EnableStatusNotifications
            };

            await _settingsService.SaveAsync(settings);

            StatusMessage = "설정을 저장했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"설정 저장 실패: {ex.Message}";
        }
    }

    private string? ValidateSettings()
    {
        if (!Uri.TryCreate(
                HttpUrl.Trim(),
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            return "HTTP 주소는 http:// 또는 https://로 시작해야 합니다.";
        }

        if (string.IsNullOrWhiteSpace(TcpHost))
        {
            return "TCP 호스트를 입력하세요.";
        }

        if (!int.TryParse(TcpPort.Trim(), out var port) ||
            port is < 1 or > 65535)
        {
            return "TCP 포트는 1부터 65535 사이의 숫자여야 합니다.";
        }

        if (!int.TryParse(
                MonitorIntervalSeconds.Trim(),
                out var intervalSeconds) ||
            intervalSeconds < 1)
        {
            return "자동 감시 주기는 1초 이상의 숫자여야 합니다.";
        }

        if (!int.TryParse(
                MaxLogLinesText.Trim(),
                out var maxLogLines) ||
            maxLogLines < 1)
        {
            return "최대 로그 줄 수는 1 이상의 숫자여야 합니다.";
        }

        return null;
    }

    public async Task ShutdownAsync()
    {
        try
        {
            if (IsMonitoring)
            {
                await _serverMonitorService.StopAsync();
                IsMonitoring = false;
            }

            if (IsLogWatching)
            {
                await _logFileWatcherService.StopAsync();
                IsLogWatching = false;
            }

            _logFileWatcherService.LineAdded -= OnLogLineAdded;
            _logFileWatcherService.WatcherError -= OnLogWatcherError;

            _logFileWatcherService.Dispose();
        }
        catch (Exception ex)
        {
            StatusMessage = $"종료 처리 중 오류가 발생했습니다: {ex.Message}";
        }
    }

    private void CheckHttpStatusChange(HttpCheckResultModel result)
    {
        if (_previousHttpSuccess is null)
        {
            _previousHttpSuccess = result.IsSuccess;
            return;
        }

        if (_previousHttpSuccess == result.IsSuccess)
        {
            return;
        }

        _previousHttpSuccess = result.IsSuccess;

        if (!EnableStatusNotifications)
        {
            return;
        }

        StatusNotificationRequested?.Invoke(
            this,
            new StatusNotificationEventArgs
            {
                Title = result.IsSuccess
                    ? "HTTP 서버 복구"
                    : "HTTP 서버 장애",
                Message = result.IsSuccess
                    ? $"{result.Url} 서버가 정상 상태로 복구되었습니다."
                    : $"{result.Url} 서버 상태 검사에 실패했습니다.\n{result.Message}",
                IsError = !result.IsSuccess
            });
    }

    private void CheckTcpStatusChange(TcpCheckResultModel result)
    {
        if (_previousTcpSuccess is null)
        {
            _previousTcpSuccess = result.IsSuccess;
            return;
        }

        if (_previousTcpSuccess == result.IsSuccess)
        {
            return;
        }

        _previousTcpSuccess = result.IsSuccess;

        if (!EnableStatusNotifications)
        {
            return;
        }

        StatusNotificationRequested?.Invoke(
            this,
            new StatusNotificationEventArgs
            {
                Title = result.IsSuccess
                    ? "TCP 서버 복구"
                    : "TCP 연결 장애",
                Message = result.IsSuccess
                    ? $"{result.TargetText} 연결이 정상 상태로 복구되었습니다."
                    : $"{result.TargetText} 연결에 실패했습니다.\n{result.Message}",
                IsError = !result.IsSuccess
            });
    }

    private async Task ShowDashboardAsync()
    {
        ChangePage("대시보드");
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            DashboardProcessCount =
                _processService.GetProcesses().Count;

            var histories =
                await _database.GetHistoriesAsync(5);

            RecentHistories.Clear();

            foreach (var history in histories)
            {
                RecentHistories.Add(history);
            }

            StatusMessage = "대시보드 정보를 새로고침했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"대시보드 조회 실패: {ex.Message}";
        }
    }
}