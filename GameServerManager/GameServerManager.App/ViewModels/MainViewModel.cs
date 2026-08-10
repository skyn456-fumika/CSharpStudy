using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using GameServerManager.App.Commands;
using GameServerManager.App.Models;
using Microsoft.Win32;
using GameServerManager.App.Services;
using GameServerManager.App.Data;

using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;

using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace GameServerManager.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private GameServerModel? _selectedServer;
    private string _serverNameInput = string.Empty;
    private string _executablePathInput = string.Empty;
    private string _argumentsInput = string.Empty;
    private string _workingDirectoryInput = string.Empty;
    private string _hostInput = "127.0.0.1";
    private string _portInput = string.Empty;
    private bool _autoRestartInput;
    private string _statusMessage =
        "GameServerManager가 준비되었습니다.";

    private readonly ServerProcessService _serverProcessService = new();

    private readonly HashSet<Guid> _manualStopServers = [];
    private readonly HashSet<Guid> _restartingServers = [];
    private readonly HashSet<Guid> _commandShutdownServers = [];
    private readonly HashSet<Guid> _activeCpuWarnings = [];
    private readonly HashSet<Guid> _activeMemoryWarnings = [];

    private readonly TcpHealthCheckService _tcpHealthCheckService = new();

    private CancellationTokenSource? _healthCheckCancellationTokenSource;

    private bool _isHealthMonitoring;

    private const int TcpFailureRestartThreshold = 3;

    private readonly ObservableCollection<ServerLogEntry>
        _selectedServerLogs = [];

    private string _logSearchText = string.Empty;

    private readonly GameServerDatabase _database = new();

    private bool _isInitialized;

    private string _startOrderInput = "0";

    private GameServerModel? _selectedDependencyServer;

    private string _serverCommandInput = string.Empty;

    //private double _cpuUsagePercent;
    //private long _memoryUsageBytes;
    private string _uptimeText = "-";

    private CancellationTokenSource? _resourceMonitorCancellationTokenSource;

    private bool _isResourceMonitoring;

    private string _cpuWarningThresholdInput = "80";
    private string _memoryWarningThresholdInput = "500";

    private readonly TrayIconService _trayIconService = new();

    public event EventHandler? ExitRequested;

    private int _totalServerCount;
    private int _runningServerCount;
    private int _healthyServerCount;
    private int _problemServerCount;
    private int _warningServerCount;

    private GameServerModel? _historyServerFilter;
    private string _historyEventFilter = "전체";
    private string _historyResultFilter = "전체";
    private int _historyRetentionDays = 30;

    private readonly AppSettingsService
    _appSettingsService = new();
    private AppSettings _appSettings = new();
    private bool _startHealthMonitoringOnLaunch = true;

    private readonly Dictionary<Guid, TcpConnectionStatus> _lastHistoryTcpStatuses = [];

    public MainViewModel()
    {
        AddServerCommand =
            new AsyncRelayCommand(AddServerAsync);
        UpdateServerCommand =
            new AsyncRelayCommand(
                UpdateServerAsync,
                () => SelectedServer is not null);

        DeleteServerCommand =
            new AsyncRelayCommand(
                DeleteServerAsync,
                () => SelectedServer is not null);

        ClearFormCommand = new RelayCommand(ClearForm);
        BrowseExecutableCommand =
            new RelayCommand(BrowseExecutable);

        StartServerCommand = new AsyncRelayCommand(
            StartServerAsync,  
            CanStartSelectedServer);

        StopServerCommand = new AsyncRelayCommand(
            StopServerAsync,
            CanStopSelectedServer);

        CheckTcpCommand = new AsyncRelayCommand(
            CheckSelectedServerTcpAsync,
            () => SelectedServer is not null);

        StartHealthMonitoringCommand = new AsyncRelayCommand(
            StartHealthMonitoringAsync,
            () => !IsHealthMonitoring);

        StopHealthMonitoringCommand = new AsyncRelayCommand(
            StopHealthMonitoringAsync,
            () => IsHealthMonitoring);

        ClearLogsCommand = new RelayCommand(ClearLogs);

        RefreshHistoriesCommand =
            new AsyncRelayCommand(LoadHistoriesAsync);
        DeleteAllHistoriesCommand =
            new AsyncRelayCommand(DeleteAllHistoriesAsync);

        StartAllServersCommand =
            new AsyncRelayCommand(
                StartAllServersAsync,
                () => Servers.Any(
                    server =>
                        server.Status is
                            ServerStatus.Stopped or
                            ServerStatus.Crashed or
                            ServerStatus.ConnectionFailed));

        StopAllServersCommand =
            new AsyncRelayCommand(
                StopAllServersAsync,
                () => Servers.Any(
                    server =>
                        server.Status == ServerStatus.Running));

        ClearDependencyCommand =
            new RelayCommand(
                () => SelectedDependencyServer = null);

        SendServerCommand =
            new AsyncRelayCommand(
                SendServerCommandAsync,
                CanSendServerCommand);

        ApplyHistoryFilterCommand =
            new AsyncRelayCommand(
                ApplyHistoryFilterAsync);

        ResetHistoryFilterCommand =
            new AsyncRelayCommand(
                ResetHistoryFilterAsync);

        _serverProcessService.ProcessExited +=
            OnServerProcessExited;
        _serverProcessService.LogReceived += OnServerLogReceived;
        _ = InitializeAsync();
        _trayIconService.ExitRequested += OnTrayExitRequested;

        Servers.CollectionChanged += (_, _) => RefreshDashboard();

    }

    public ObservableCollection<GameServerModel> Servers { get; } = [];

    public ObservableCollection<ServerLogEntry> ServerLogs { get; } = [];

    public ObservableCollection<ServerHistoryEntry> ServerHistories { get; } = [];

    public ObservableCollection<GameServerModel> AvailableDependencyServers { get; } = [];

    public AsyncRelayCommand AddServerCommand { get; }

    public AsyncRelayCommand UpdateServerCommand { get; }

    public AsyncRelayCommand DeleteServerCommand { get; }

    public RelayCommand ClearFormCommand { get; }

    public RelayCommand BrowseExecutableCommand { get; }

    public AsyncRelayCommand StartServerCommand { get; }

    public AsyncRelayCommand StopServerCommand { get; }

    public AsyncRelayCommand CheckTcpCommand { get; }

    public AsyncRelayCommand StartHealthMonitoringCommand { get; }

    public AsyncRelayCommand StopHealthMonitoringCommand { get; }

    public RelayCommand ClearLogsCommand { get; }

    public AsyncRelayCommand RefreshHistoriesCommand { get; }

    public AsyncRelayCommand DeleteAllHistoriesCommand { get; }

    public AsyncRelayCommand StartAllServersCommand { get; }

    public AsyncRelayCommand StopAllServersCommand { get; }

    public RelayCommand ClearDependencyCommand { get; }

    public AsyncRelayCommand SendServerCommand { get; }

    public IReadOnlyList<string> HistoryEventFilters { get; } =
    [
        "전체",
        "START",
        "STOP",
        "CRASH",
        "AUTO_RESTART",
        "TCP_RESTART",
        "TCP_CHECK",
        "COMMAND",
        "SHUTDOWN_COMMAND",
        "CPU_WARNING",
        "CPU_RECOVERED",
        "MEMORY_WARNING",
        "MEMORY_RECOVERED"
    ];

    public IReadOnlyList<string> HistoryResultFilters { get; } =
    [
        "전체",
    "성공",
    "실패"
    ];

    public AsyncRelayCommand ApplyHistoryFilterCommand { get; }

    public AsyncRelayCommand ResetHistoryFilterCommand { get; }

    public bool IsHealthMonitoring
    {
        get => _isHealthMonitoring;
        private set
        {
            if (SetProperty(ref _isHealthMonitoring, value))
            {
                OnPropertyChanged(nameof(HealthMonitoringStatusText));

                StartHealthMonitoringCommand.RaiseCanExecuteChanged();
                StopHealthMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string HealthMonitoringStatusText =>
        IsHealthMonitoring
            ? "TCP 자동 감시 중"
            : "TCP 자동 감시 중지";

    public GameServerModel? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (!SetProperty(ref _selectedServer, value))
            {
                return;
            }

            UpdateServerCommand.RaiseCanExecuteChanged();
            DeleteServerCommand.RaiseCanExecuteChanged();
            StartServerCommand.RaiseCanExecuteChanged();
            StopServerCommand.RaiseCanExecuteChanged();
            CheckTcpCommand.RaiseCanExecuteChanged();
            SendServerCommand.RaiseCanExecuteChanged();

            RefreshAvailableDependencyServers();

            if (value is not null)
            {
                LoadSelectedServer(value);
            }

            RefreshSelectedServerLogs();
        }
    }

    public string ServerNameInput
    {
        get => _serverNameInput;
        set => SetProperty(ref _serverNameInput, value);
    }

    public string ExecutablePathInput
    {
        get => _executablePathInput;
        set => SetProperty(ref _executablePathInput, value);
    }

    public string ArgumentsInput
    {
        get => _argumentsInput;
        set => SetProperty(ref _argumentsInput, value);
    }

    public string WorkingDirectoryInput
    {
        get => _workingDirectoryInput;
        set => SetProperty(ref _workingDirectoryInput, value);
    }

    public string HostInput
    {
        get => _hostInput;
        set => SetProperty(ref _hostInput, value);
    }

    public string PortInput
    {
        get => _portInput;
        set => SetProperty(ref _portInput, value);
    }

    public bool AutoRestartInput
    {
        get => _autoRestartInput;
        set => SetProperty(ref _autoRestartInput, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<ServerLogEntry> SelectedServerLogs =>
        _selectedServerLogs;

    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            if (SetProperty(ref _logSearchText, value))
            {
                RefreshSelectedServerLogs();
            }
        }
    }

    public string StartOrderInput
    {
        get => _startOrderInput;
        set => SetProperty(ref _startOrderInput, value);
    }

    public GameServerModel? SelectedDependencyServer
    {
        get => _selectedDependencyServer;
        set => SetProperty(
            ref _selectedDependencyServer,
            value);
    }

    public string ServerCommandInput
    {
        get => _serverCommandInput;
        set => SetProperty(
            ref _serverCommandInput,
            value);
    }

    public string UptimeText
    {
        get => _uptimeText;
        set => SetProperty(ref _uptimeText, value);
    }

    public bool IsResourceMonitoring
    {
        get => _isResourceMonitoring;
        private set => SetProperty(
            ref _isResourceMonitoring,
            value);
    }

    public string CpuWarningThresholdInput
    {
        get => _cpuWarningThresholdInput;
        set => SetProperty(
            ref _cpuWarningThresholdInput,
            value);
    }

    public string MemoryWarningThresholdInput
    {
        get => _memoryWarningThresholdInput;
        set => SetProperty(
            ref _memoryWarningThresholdInput,
            value);
    }

    public bool HasRunningServers =>
        Servers.Any(
            server =>
                server.Status == ServerStatus.Running);

    public int TotalServerCount
    {
        get => _totalServerCount;
        private set => SetProperty(ref _totalServerCount, value);
    }

    public int RunningServerCount
    {
        get => _runningServerCount;
        private set => SetProperty(ref _runningServerCount, value);
    }

    public int HealthyServerCount
    {
        get => _healthyServerCount;
        private set => SetProperty(ref _healthyServerCount, value);
    }

    public int ProblemServerCount
    {
        get => _problemServerCount;
        private set => SetProperty(ref _problemServerCount, value);
    }

    public int WarningServerCount
    {
        get => _warningServerCount;
        private set => SetProperty(ref _warningServerCount, value);
    }

    public GameServerModel? HistoryServerFilter
    {
        get => _historyServerFilter;
        set => SetProperty(ref _historyServerFilter, value);
    }

    public string HistoryEventFilter
    {
        get => _historyEventFilter;
        set => SetProperty(ref _historyEventFilter, value);
    }

    public string HistoryResultFilter
    {
        get => _historyResultFilter;
        set => SetProperty(ref _historyResultFilter, value);
    }

    public int HistoryRetentionDays
    {
        get => _historyRetentionDays;
        set
        {
            if (!SetProperty(
                    ref _historyRetentionDays,
                    value))
            {
                return;
            }

            _appSettings.HistoryRetentionDays = value;

            _ = SaveAppSettingsAsync();
        }
    }

    public bool StartHealthMonitoringOnLaunch
    {
        get => _startHealthMonitoringOnLaunch;
        set
        {
            if (!SetProperty(
                    ref _startHealthMonitoringOnLaunch,
                    value))
            {
                return;
            }

            _appSettings.StartHealthMonitoringOnLaunch =
                value;

            _ = SaveAppSettingsAsync();
        }
    }

    private async Task AddServerAsync()
    {
        if (!TryValidateInput(
            out var port,
            out var startOrder,
            out var cpuWarningThreshold,
            out var memoryWarningThresholdMb))
        {
            return;
        }

        var duplicateName = Servers.Any(
            server => string.Equals(
                server.ServerName,
                ServerNameInput.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (duplicateName)
        {
            StatusMessage = "같은 이름의 서버가 이미 등록되어 있습니다.";
            return;
        }

        var host =
            HostInput.Trim();

        if (HasDuplicateEndpoint(
                host,
                port))
        {
            StatusMessage =
                $"{host}:{port}를 사용하는 서버가 이미 등록되어 있습니다.";

            return;
        }

        var server = new GameServerModel
        {
            ServerName = ServerNameInput.Trim(),
            ExecutablePath = ExecutablePathInput.Trim(),
            Arguments = ArgumentsInput.Trim(),
            WorkingDirectory = WorkingDirectoryInput.Trim(),
            Host = host,
            Port = port,
            AutoRestart = AutoRestartInput,
            StartOrder = startOrder,
            DependencyServerId = SelectedDependencyServer?.Id,
            DependencyServerName = SelectedDependencyServer?.ServerName ?? "없음",
            CpuWarningThreshold =
                cpuWarningThreshold,
            MemoryWarningThresholdMb =
                memoryWarningThresholdMb
        };

        try
        {
            await _database.SaveServerAsync(server);
            Servers.Add(server);

            StatusMessage =
                $"{server.ServerName} 서버를 등록했습니다.";

            ClearForm();
            RefreshAvailableDependencyServers();
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"서버 등록 실패: {ex.Message}";
        }
    }

    private async Task UpdateServerAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "수정할 서버를 선택하세요.";
            return;
        }

        if (SelectedServer.Status is
            ServerStatus.Running or
            ServerStatus.Starting or
            ServerStatus.Stopping)
        {
            StatusMessage =
                "실행 중인 서버 정보는 수정할 수 없습니다.";

            return;
        }

        if (!TryValidateInput(
            out var port,
            out var startOrder,
            out var cpuWarningThreshold,
            out var memoryWarningThresholdMb))
{
            return;
        }

        var duplicateName = Servers.Any(
            server =>
                server.Id != SelectedServer.Id &&
                string.Equals(
                    server.ServerName,
                    ServerNameInput.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (duplicateName)
        {
            StatusMessage = "같은 이름의 서버가 이미 등록되어 있습니다.";
            return;
        }

        var host =
            HostInput.Trim();

        if (HasDuplicateEndpoint(
                host,
                port,
                SelectedServer.Id))
        {
            StatusMessage =
                $"{host}:{port}를 사용하는 다른 서버가 이미 있습니다.";

            return;
        }

        try
        {
            SelectedServer.ServerName =
                ServerNameInput.Trim();

            SelectedServer.ExecutablePath =
                ExecutablePathInput.Trim();

            SelectedServer.Arguments =
                ArgumentsInput.Trim();

            SelectedServer.WorkingDirectory =
                WorkingDirectoryInput.Trim();

            SelectedServer.Host =
                HostInput.Trim();

            SelectedServer.Port = port;
            SelectedServer.AutoRestart = AutoRestartInput;
            SelectedServer.StartOrder = startOrder;

            SelectedServer.DependencyServerId = SelectedDependencyServer?.Id;
            SelectedServer.DependencyServerName = SelectedDependencyServer?.ServerName ?? "없음";

            SelectedServer.CpuWarningThreshold =
                cpuWarningThreshold;

            SelectedServer.MemoryWarningThresholdMb =
                memoryWarningThresholdMb;

            SelectedServer.TcpStatus =
                TcpConnectionStatus.NotChecked;

            SelectedServer.ResponseTimeMs = null;
            SelectedServer.ConsecutiveTcpFailures = 0;

            if (HasCircularDependency(
                SelectedServer,
                SelectedDependencyServer?.Id))
            {
                StatusMessage =
                    "서버 의존 관계가 순환 구조를 만들 수 없습니다.";

                return;
            }

            UpdateOverallStatus(SelectedServer);

            await _database.SaveServerAsync(SelectedServer);

            StatusMessage =
                $"{SelectedServer.ServerName} 서버 정보를 수정했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"서버 수정 실패: {ex.Message}";
        }
    }

    private async Task DeleteServerAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "삭제할 서버를 선택하세요.";
            return;
        }

        if (SelectedServer.Status is
            ServerStatus.Running or
            ServerStatus.Starting or
            ServerStatus.Stopping)
        {
            StatusMessage =
                "실행 중이거나 상태가 변경 중인 서버는 삭제할 수 없습니다.";

            return;
        }

        var dependentServers =
            Servers
                .Where(server =>
                    server.DependencyServerId ==
                    SelectedServer.Id)
                .ToList();

        if (dependentServers.Count > 0)
        {
            var names =
                string.Join(
                    ", ",
                    dependentServers.Select(
                        server => server.ServerName));

            StatusMessage =
                $"{SelectedServer.ServerName} 서버를 " +
                $"선행 서버로 사용하는 서버가 있습니다: {names}";

            System.Windows.MessageBox.Show(
                $"{SelectedServer.ServerName} 서버를 삭제할 수 없습니다.\n\n" +
                $"다음 서버가 이 서버를 선행 서버로 사용하고 있습니다.\n" +
                $"{names}\n\n" +
                "먼저 해당 서버의 선행 서버 지정을 해제하세요.",
                "서버 삭제 불가",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var result = WpfMessageBox.Show(
            $"{SelectedServer.ServerName} 서버를 삭제하시겠습니까?",
            "서버 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var server = SelectedServer;

        try
        {
            await _database.DeleteServerAsync(server.Id);

            Servers.Remove(server);
            SelectedServer = null;

            ClearForm();

            StatusMessage =
                $"{server.ServerName} 서버를 삭제했습니다.";
            RefreshAvailableDependencyServers();
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"서버 삭제 실패: {ex.Message}";
        }
    }

    private void BrowseExecutable()
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "게임 서버 실행 파일 선택",
            Filter =
                "실행 파일 (*.exe)|*.exe|" +
                "모든 파일 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ExecutablePathInput = dialog.FileName;

        var directory =
            Path.GetDirectoryName(dialog.FileName);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            WorkingDirectoryInput = directory;
        }

        if (string.IsNullOrWhiteSpace(ServerNameInput))
        {
            ServerNameInput =
                Path.GetFileNameWithoutExtension(dialog.FileName);
        }

        StatusMessage = "실행 파일을 선택했습니다.";
    }

    private void LoadSelectedServer(GameServerModel server)
    {
        ServerNameInput = server.ServerName;
        ExecutablePathInput = server.ExecutablePath;
        ArgumentsInput = server.Arguments;
        WorkingDirectoryInput = server.WorkingDirectory;
        HostInput = server.Host;
        PortInput = server.Port.ToString();
        AutoRestartInput = server.AutoRestart;
        StartOrderInput = server.StartOrder.ToString();
        SelectedDependencyServer =
            server.DependencyServerId is null
                ? null
                : AvailableDependencyServers.FirstOrDefault(
                    item =>
                        item.Id == server.DependencyServerId.Value);
        CpuWarningThresholdInput =
            server.CpuWarningThreshold.ToString("0.##");
        MemoryWarningThresholdInput =
            server.MemoryWarningThresholdMb.ToString("0.##");

        StatusMessage =
            $"{server.ServerName} 서버를 선택했습니다.";
    }

    private void ClearForm()
    {
        SelectedServer = null;

        ServerNameInput = string.Empty;
        ExecutablePathInput = string.Empty;
        ArgumentsInput = string.Empty;
        WorkingDirectoryInput = string.Empty;
        HostInput = "127.0.0.1";
        PortInput = string.Empty;
        AutoRestartInput = false;
        SelectedDependencyServer = null;
        CpuWarningThresholdInput = "80";
        MemoryWarningThresholdInput = "500";

        RefreshServerCommandStates();

        StatusMessage = "입력 항목을 초기화했습니다.";
    }

    private bool TryValidateInput(
        out int port,
        out int startOrder,
        out double cpuWarningThreshold,
        out double memoryWarningThresholdMb)
    {
        port = 0;
        startOrder = 0;
        cpuWarningThreshold = 0;
        memoryWarningThresholdMb = 0;

        if (string.IsNullOrWhiteSpace(ServerNameInput))
        {
            StatusMessage = "서버 이름을 입력하세요.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ExecutablePathInput))
        {
            StatusMessage = "실행 파일을 선택하세요.";
            return false;
        }

        if (!File.Exists(ExecutablePathInput.Trim()))
        {
            StatusMessage = "선택한 실행 파일이 존재하지 않습니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(WorkingDirectoryInput))
        {
            StatusMessage = "작업 폴더를 입력하세요.";
            return false;
        }

        if (!Directory.Exists(WorkingDirectoryInput.Trim()))
        {
            StatusMessage = "작업 폴더가 존재하지 않습니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(HostInput))
        {
            StatusMessage = "호스트를 입력하세요.";
            return false;
        }

        if (!int.TryParse(PortInput, out port) ||
            port is < 1 or > 65535)
        {
            StatusMessage =
                "포트는 1부터 65535 사이의 숫자여야 합니다.";

            return false;
        }

        if (!int.TryParse(StartOrderInput, out startOrder) ||
            startOrder < 0)
        {
            StatusMessage =
                "실행 순서는 0 이상의 숫자여야 합니다.";

            return false;
        }

        if (!double.TryParse(
                CpuWarningThresholdInput,
                out cpuWarningThreshold) ||
            cpuWarningThreshold is < 1 or > 100)
        {
            StatusMessage =
                "CPU 경고 기준은 1부터 100 사이여야 합니다.";

            return false;
        }

        if (!double.TryParse(
                MemoryWarningThresholdInput,
                out memoryWarningThresholdMb) ||
            memoryWarningThresholdMb <= 0)
        {
            StatusMessage =
                "메모리 경고 기준은 0보다 커야 합니다.";

            return false;
        }

        return true;
    }

    private async Task StartServerAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "실행할 서버를 선택하세요.";
            return;
        }

        var server = SelectedServer;

        var success = await StartServerCoreAsync(server);

        if (success)
        {
            StatusMessage =
                $"{server.ServerName} 서버를 시작했습니다. " +
                $"PID: {server.ProcessId}";
        }
    }

    private async Task StopServerAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "종료할 서버를 선택하세요.";
            return;
        }

        var server = SelectedServer;

        var result = WpfMessageBox.Show(
            $"{server.ServerName} 서버를 종료하시겠습니까?",
            "서버 종료",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var success = await StopServerCoreAsync(server);

        if (success)
        {
            StatusMessage =
                $"{server.ServerName} 서버를 종료했습니다.";
        }
    }

    private bool CanStartSelectedServer()
    {
        return SelectedServer is not null &&
               SelectedServer.Status is ServerStatus.Stopped
                   or ServerStatus.Crashed
                   or ServerStatus.ConnectionFailed;
    }

    private bool CanStopSelectedServer()
    {
        return SelectedServer is not null &&
               SelectedServer.Status == ServerStatus.Running;
    }

    private void RefreshServerCommandStates()
    {
        StartServerCommand.RaiseCanExecuteChanged();
        StopServerCommand.RaiseCanExecuteChanged();
        UpdateServerCommand.RaiseCanExecuteChanged();
        DeleteServerCommand.RaiseCanExecuteChanged();
        StartAllServersCommand.RaiseCanExecuteChanged();
        StopAllServersCommand.RaiseCanExecuteChanged();
        SendServerCommand.RaiseCanExecuteChanged();
    }

    public async Task ShutdownAsync()
    {
        if (_resourceMonitorCancellationTokenSource is not null)
        {
            _resourceMonitorCancellationTokenSource.Cancel();
            _resourceMonitorCancellationTokenSource.Dispose();
            _resourceMonitorCancellationTokenSource = null;
        }

        if (IsHealthMonitoring)
        {
            await StopHealthMonitoringAsync();
        }

        var runningServers = Servers
            .Where(server =>
                server.Status == ServerStatus.Running)
            .ToList();

        foreach (var server in runningServers)
        {
            try
            {
                server.Status = ServerStatus.Stopping;
                _manualStopServers.Add(server.Id);

                await _serverProcessService.StopAsync(
                    server,
                    TimeSpan.FromSeconds(2));

                server.ProcessId = null;
                server.Status = ServerStatus.Stopped;
            }
            catch
            {
                // 애플리케이션 종료 중에는 개별 종료 오류를 무시한다.
            }
        }

        _serverProcessService.ProcessExited -=
            OnServerProcessExited;

        _serverProcessService.LogReceived -= OnServerLogReceived;
        _trayIconService.ExitRequested -= OnTrayExitRequested;

        _commandShutdownServers.Clear();
        _manualStopServers.Clear();
        _restartingServers.Clear();

        _trayIconService.Dispose();

        _serverProcessService.Dispose();
    }

    private void OnServerProcessExited(
        object? sender,
        ServerProcessExitedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(
            async () =>
            {
                var server = Servers.FirstOrDefault(
                    item => item.Id == e.ServerId);

                if (server is null)
                {
                    return;
                }

                server.ProcessId = null;

                _lastHistoryTcpStatuses.Remove(server.Id);

                server.TcpStatus =
                    TcpConnectionStatus.NotChecked;
                server.ResponseTimeMs = null;
                server.ConsecutiveTcpFailures = 0;

                // 종료 버튼 또는 전체 종료로 종료한 경우
                if (_manualStopServers.Remove(server.Id))
                {
                    server.Status = ServerStatus.Stopped;

                    UpdateOverallStatus(server);
                    RefreshServerCommandStates();

                    return;
                }

                // shutdown 콘솔 명령으로 종료한 경우
                if (_commandShutdownServers.Remove(server.Id))
                {
                    server.Status = ServerStatus.Stopped;

                    UpdateOverallStatus(server);
                    RefreshServerCommandStates();

                    StatusMessage =
                        $"{server.ServerName} 서버가 shutdown " +
                        "명령으로 정상 종료되었습니다.";

                    await AddHistoryAsync(
                        server,
                        "SHUTDOWN_COMMAND",
                        true,
                        "shutdown 명령으로 서버가 정상 종료되었습니다.");

                    return;
                }

                // 그 외 종료는 비정상 종료
                server.Status = ServerStatus.Crashed;

                UpdateOverallStatus(server);
                //ResetServerResourceUsage(server);

                StatusMessage =
                    $"{server.ServerName} 서버가 비정상 종료되었습니다. " +
                    $"종료 코드: {e.ExitCode}";

                _trayIconService.ShowError(
                    "게임 서버 비정상 종료",
                    $"{server.ServerName} 서버가 비정상 종료되었습니다. " +
                    $"종료 코드: {e.ExitCode}");

                await AddHistoryAsync(
                    server,
                    "CRASH",
                    false,
                    $"프로세스 비정상 종료. 종료 코드: {e.ExitCode}");

                RefreshServerCommandStates();

                if (!server.AutoRestart)
                {
                    return;
                }

                await RestartServerAsync(server);
            });
    }

    private async Task RestartServerAsync(GameServerModel server)
    {
        if (!_restartingServers.Add(server.Id))
        {
            return;
        }

        try
        {
            StatusMessage =
                $"{server.ServerName} 서버를 3초 후 자동 재시작합니다.";

            await Task.Delay(TimeSpan.FromSeconds(3));

            if (server.Status != ServerStatus.Crashed)
            {
                return;
            }

            server.Status = ServerStatus.Starting;
            RefreshServerCommandStates();

            var process = _serverProcessService.Start(server);

            server.ProcessId = process.Id;
            server.Status = ServerStatus.Running;
            server.TcpStatus = TcpConnectionStatus.NotChecked;
            server.ResponseTimeMs = null;
            server.ConsecutiveTcpFailures = 0;
            server.LastRestartedAt = DateTime.Now;

            UpdateOverallStatus(server);

            StatusMessage =
                $"{server.ServerName} 서버가 자동 재시작되었습니다. " +
                $"PID: {process.Id}";

            _trayIconService.ShowInformation(
                "게임 서버 자동 재시작",
                $"{server.ServerName} 서버가 자동으로 재시작되었습니다. " +
                $"PID: {server.ProcessId}");

            await AddHistoryAsync(
                server,
                "AUTO_RESTART",
                true,
                $"비정상 종료 후 자동 재시작. PID: {process.Id}");
        }
        catch (Exception ex)
        {
            server.ProcessId = null;
            server.Status = ServerStatus.Crashed;

            StatusMessage =
                $"{server.ServerName} 서버 자동 재시작 실패: {ex.Message}";

            await AddHistoryAsync(
                server,
                "AUTO_RESTART",
                false,
                ex.Message);
        }
        finally
        {
            _restartingServers.Remove(server.Id);
            RefreshServerCommandStates();
        }
    }

    private async Task CheckSelectedServerTcpAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "검사할 서버를 선택하세요.";
            return;
        }

        await CheckServerTcpAsync(
            SelectedServer,
            CancellationToken.None);
    }

    private async Task CheckServerTcpAsync(
        GameServerModel server,
        CancellationToken cancellationToken)
    {
        try
        {
            server.TcpStatus =
                TcpConnectionStatus.Checking;

            var result =
                await _tcpHealthCheckService.CheckAsync(
                    server.Host,
                    server.Port,
                    TimeSpan.FromSeconds(2));

            server.ResponseTimeMs =
                result.ResponseTimeMs;

            server.LastCheckedAt =
                DateTime.Now;

            if (result.IsSuccess)
            {
                server.TcpStatus =
                    TcpConnectionStatus.Connected;

                server.ConsecutiveTcpFailures = 0;
            }
            else
            {
                server.TcpStatus =
                    TcpConnectionStatus.Failed;

                server.ConsecutiveTcpFailures++;
            }

            // 성공/실패 상태를 최종 반영한 뒤 호출
            await SaveTcpHistoryIfChangedAsync(server);

            UpdateOverallStatus(server);

            // 기존 TCP 연속 실패 재시작 처리
            if (!result.IsSuccess &&
                server.ConsecutiveTcpFailures >= 3 &&
                server.AutoRestart)
            {
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            server.TcpStatus =
                TcpConnectionStatus.Failed;

            server.ResponseTimeMs = null;
            server.LastCheckedAt =
                DateTime.Now;

            server.ConsecutiveTcpFailures++;

            // 예외가 나도 실패 이력 기록
            await SaveTcpHistoryIfChangedAsync(server);

            UpdateOverallStatus(server);

            StatusMessage =
                $"{server.ServerName} TCP 검사 실패: " +
                ex.Message;
        }
    }

    private Task StartHealthMonitoringAsync()
    {
        if (IsHealthMonitoring)
        {
            return Task.CompletedTask;
        }

        _healthCheckCancellationTokenSource =
            new CancellationTokenSource();

        IsHealthMonitoring = true;

        _ = RunHealthMonitoringAsync(
            _healthCheckCancellationTokenSource.Token);

        StatusMessage = "TCP 자동 감시를 시작했습니다.";

        return Task.CompletedTask;
    }

    private async Task RunHealthMonitoringAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var timer =
                new PeriodicTimer(TimeSpan.FromSeconds(5));

            await CheckAllServersTcpAsync(cancellationToken);

            while (await timer.WaitForNextTickAsync(
                       cancellationToken))
            {
                await CheckAllServersTcpAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 감시 중지
        }
        finally
        {
            IsHealthMonitoring = false;
        }
    }

    private async Task CheckAllServersTcpAsync(
        CancellationToken cancellationToken)
    {
        var servers = Servers.ToList();

        var tasks = servers.Select(
            server => CheckServerTcpAsync(
                server,
                cancellationToken));

        await Task.WhenAll(tasks);
    }

    private Task StopHealthMonitoringAsync()
    {
        if (!IsHealthMonitoring)
        {
            return Task.CompletedTask;
        }

        _healthCheckCancellationTokenSource?.Cancel();
        _healthCheckCancellationTokenSource?.Dispose();
        _healthCheckCancellationTokenSource = null;

        IsHealthMonitoring = false;
        StatusMessage = "TCP 자동 감시를 중지했습니다.";

        return Task.CompletedTask;
    }

    private void UpdateOverallStatus(GameServerModel server)
    {
        server.OverallStatus = server.Status switch
        {
            ServerStatus.Starting =>
                ServerOverallStatus.Starting,

            ServerStatus.Stopping =>
                ServerOverallStatus.Stopped,

            ServerStatus.Crashed =>
                ServerOverallStatus.Crashed,

            ServerStatus.Stopped =>
                ServerOverallStatus.Stopped,

            ServerStatus.Running
                when server.TcpStatus ==
                     TcpConnectionStatus.Connected =>
                ServerOverallStatus.Healthy,

            ServerStatus.Running
                when server.TcpStatus ==
                     TcpConnectionStatus.Failed =>
                ServerOverallStatus.Unreachable,

            ServerStatus.Running =>
                ServerOverallStatus.ProcessOnly,

            _ => ServerOverallStatus.Stopped
        };

        RefreshDashboard();
    }

    private async Task RestartServerAfterTcpFailureAsync(
        GameServerModel server)
    {
        if (!_restartingServers.Add(server.Id))
        {
            return;
        }

        try
        {
            server.OverallStatus =
                ServerOverallStatus.Restarting;

            StatusMessage =
                $"{server.ServerName} 서버의 TCP 연결이 " +
                $"{TcpFailureRestartThreshold}회 연속 실패하여 " +
                "자동 재시작합니다.";

            _manualStopServers.Add(server.Id);

            await _serverProcessService.StopAsync(
                server,
                TimeSpan.FromSeconds(3));

            server.ProcessId = null;
            server.Status = ServerStatus.Stopped;

            await Task.Delay(TimeSpan.FromSeconds(2));

            server.Status = ServerStatus.Starting;
            server.OverallStatus =
                ServerOverallStatus.Restarting;

            var process =
                _serverProcessService.Start(server);

            server.ProcessId = process.Id;
            server.Status = ServerStatus.Running;
            server.TcpStatus = TcpConnectionStatus.NotChecked;
            server.ResponseTimeMs = null;
            server.ConsecutiveTcpFailures = 0;
            server.LastRestartedAt = DateTime.Now;

            UpdateOverallStatus(server);

            StatusMessage =
                $"{server.ServerName} 서버가 TCP 장애로 " +
                $"자동 재시작되었습니다. PID: {process.Id}";

            await AddHistoryAsync(
                server,
                "TCP_RESTART",
                true,
                $"TCP 연속 실패로 자동 재시작. PID: {process.Id}");
        }
        catch (Exception ex)
        {
            server.ProcessId = null;
            server.Status = ServerStatus.Crashed;
            server.OverallStatus =
                ServerOverallStatus.Crashed;

            StatusMessage =
                $"{server.ServerName} 서버 자동 재시작 실패: " +
                ex.Message;

            await AddHistoryAsync(
                server,
                "TCP_RESTART",
                false,
                ex.Message);
        }
        finally
        {
            _restartingServers.Remove(server.Id);
            RefreshServerCommandStates();
        }
    }

    private void OnServerLogReceived(
        object? sender,
        ServerLogEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var logEntry = new ServerLogEntry
            {
                ServerId = e.ServerId,
                ServerName = e.ServerName,
                Level = e.Level,
                Message = e.Message,
                CreatedAt = e.CreatedAt
            };

            ServerLogs.Add(logEntry);

            const int maxLogCount = 5000;

            while (ServerLogs.Count > maxLogCount)
            {
                ServerLogs.RemoveAt(0);
            }

            if (SelectedServer?.Id == e.ServerId &&
                MatchesLogSearch(logEntry))
            {
                SelectedServerLogs.Add(logEntry);
            }
        });
    }

    private void RefreshSelectedServerLogs()
    {
        SelectedServerLogs.Clear();

        if (SelectedServer is null)
        {
            return;
        }

        var logs = ServerLogs
            .Where(log =>
                log.ServerId == SelectedServer.Id &&
                MatchesLogSearch(log))
            .OrderBy(log => log.CreatedAt);

        foreach (var log in logs)
        {
            SelectedServerLogs.Add(log);
        }
    }

    private bool MatchesLogSearch(ServerLogEntry log)
    {
        if (string.IsNullOrWhiteSpace(LogSearchText))
        {
            return true;
        }

        var keyword = LogSearchText.Trim();

        return log.Message.Contains(
                   keyword,
                   StringComparison.OrdinalIgnoreCase) ||
               log.Level.Contains(
                   keyword,
                   StringComparison.OrdinalIgnoreCase);
    }

    private void ClearLogs()
    {
        if (SelectedServer is null)
        {
            StatusMessage = "로그를 삭제할 서버를 선택하세요.";
            return;
        }

        var serverId = SelectedServer.Id;

        var logsToRemove = ServerLogs
            .Where(log => log.ServerId == serverId)
            .ToList();

        foreach (var log in logsToRemove)
        {
            ServerLogs.Remove(log);
        }

        SelectedServerLogs.Clear();

        StatusMessage =
            $"{SelectedServer.ServerName} 서버 로그를 삭제했습니다.";
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _database.InitializeAsync();

            await LoadAppSettingsAsync();

            await CleanupOldHistoriesAsync();

            await LoadServersAsync();
            await LoadHistoriesAsync();

            RefreshDashboard();

            _isInitialized = true;

            StartResourceMonitoring();

            if (StartHealthMonitoringOnLaunch)
            {
                await StartHealthMonitoringAsync();
            }

            StatusMessage =
                $"서버 {Servers.Count}개와 이력 " +
                $"{ServerHistories.Count}건을 불러왔습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"데이터베이스 초기화 실패: {ex.Message}";
        }
    }

    private async Task AddHistoryAsync(
        GameServerModel server,
        string eventType,
        bool isSuccess,
        string message)
    {
        if (!_isInitialized)
        {
            return;
        }

        var history = new ServerHistoryEntry
        {
            ServerId = server.Id,
            ServerName = server.ServerName,
            EventType = eventType,
            IsSuccess = isSuccess,
            Message = message,
            CreatedAt = DateTime.Now
        };

        await _database.AddHistoryAsync(history);

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(
            () =>
            {
                if (MatchesCurrentHistoryFilter(history))
                {
                    ServerHistories.Insert(0, history);
                }

                const int maxVisibleHistories = 500;

                while (ServerHistories.Count >
                       maxVisibleHistories)
                {
                    ServerHistories.RemoveAt(
                        ServerHistories.Count - 1);
                }
            });
    }

    private async Task LoadHistoriesAsync()
    {
        try
        {
            var histories =
                await _database.GetHistoriesAsync(limit: 500);

            ServerHistories.Clear();

            foreach (var history in histories)
            {
                ServerHistories.Add(history);
            }

            StatusMessage =
                $"서버 이력 {ServerHistories.Count}건을 불러왔습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"이력 조회 실패: {ex.Message}";
        }
    }

    private async Task DeleteAllHistoriesAsync()
    {
        var result = WpfMessageBox.Show(
            "저장된 서버 이력을 모두 삭제하시겠습니까?",
            "서버 이력 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _database.DeleteAllHistoriesAsync();

            ServerHistories.Clear();

            StatusMessage =
                "서버 이력을 모두 삭제했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"서버 이력 삭제 실패: {ex.Message}";
        }
    }

    private async Task LoadServersAsync()
    {
        var servers = await _database.GetServersAsync();

        Servers.Clear();

        foreach (var server in servers)
        {
            Servers.Add(server);
        }
    }

    private async Task<bool> StartServerCoreAsync(
        GameServerModel server,
        bool addHistory = true)
    {
        if (server.Status is
            ServerStatus.Running or
            ServerStatus.Starting)
        {
            return true;
        }

        if (!File.Exists(server.ExecutablePath))
        {
            StatusMessage =
                $"{server.ServerName} 실행 파일을 찾을 수 없습니다.";

            await AddHistoryAsync(
                server,
                "START",
                false,
                $"실행 파일 없음: {server.ExecutablePath}");

            return false;
        }

        if (!Directory.Exists(server.WorkingDirectory))
        {
            StatusMessage =
                $"{server.ServerName} 작업 폴더를 찾을 수 없습니다.";

            await AddHistoryAsync(
                server,
                "START",
                false,
                $"작업 폴더 없음: {server.WorkingDirectory}");

            return false;
        }

        try
        {
            _commandShutdownServers.Remove(server.Id);
            _manualStopServers.Remove(server.Id);

            server.Status = ServerStatus.Starting;
            UpdateOverallStatus(server);
            RefreshServerCommandStates();

            var process =
                _serverProcessService.Start(server);

            server.ProcessId = process.Id;
            server.Status = ServerStatus.Running;
            server.TcpStatus =
                TcpConnectionStatus.NotChecked;

            server.ResponseTimeMs = null;
            server.ConsecutiveTcpFailures = 0;

            _lastHistoryTcpStatuses.Remove(server.Id);

            UpdateOverallStatus(server);

            if (addHistory)
            {
                await AddHistoryAsync(
                    server,
                    "START",
                    true,
                    $"서버 시작 성공. PID: {process.Id}");
            }

            return true;
        }
        catch (Exception ex)
        {
            server.ProcessId = null;
            server.Status = ServerStatus.Stopped;

            UpdateOverallStatus(server);

            if (addHistory)
            {
                await AddHistoryAsync(
                    server,
                    "START",
                    false,
                    ex.Message);
            }

            StatusMessage =
                $"{server.ServerName} 서버 시작 실패: {ex.Message}";

            return false;
        }
        finally
        {
            RefreshServerCommandStates();
        }
    }

    private async Task<bool> StopServerCoreAsync(
        GameServerModel server,
        bool addHistory = true)
    {
        if (server.Status != ServerStatus.Running)
        {
            return true;
        }

        try
        {
            server.Status = ServerStatus.Stopping;
            RefreshServerCommandStates();

            _manualStopServers.Add(server.Id);

            await _serverProcessService.StopAsync(
                server,
                TimeSpan.FromSeconds(3));

            server.ProcessId = null;

            _lastHistoryTcpStatuses.Remove(server.Id);

            server.Status = ServerStatus.Stopped;
            server.TcpStatus =
                TcpConnectionStatus.NotChecked;
            server.ResponseTimeMs = null;
            server.ConsecutiveTcpFailures = 0;

            UpdateOverallStatus(server);
            //ResetServerResourceUsage(server);

            if (addHistory)
            {
                await AddHistoryAsync(
                    server,
                    "STOP",
                    true,
                    "서버가 정상 종료되었습니다.");
            }

            return true;
        }
        catch (Exception ex)
        {
            if (_serverProcessService.IsRunning(server))
            {
                server.Status = ServerStatus.Running;
            }
            else
            {
                server.ProcessId = null;
                server.Status = ServerStatus.Stopped;
            }

            UpdateOverallStatus(server);

            if (addHistory)
            {
                await AddHistoryAsync(
                    server,
                    "STOP",
                    false,
                    ex.Message);
            }

            StatusMessage =
                $"{server.ServerName} 서버 종료 실패: {ex.Message}";

            return false;
        }
        finally
        {
            RefreshServerCommandStates();
        }
    }

    private async Task StartAllServersAsync()
    {
        var targets = Servers
            .OrderBy(server => server.StartOrder)
            .ThenBy(server => server.ServerName)
            .ToList();

        if (targets.Count == 0)
        {
            StatusMessage = "등록된 서버가 없습니다.";
            return;
        }

        var startedCount = 0;

        foreach (var server in targets)
        {
            if (server.Status == ServerStatus.Running)
            {
                continue;
            }

            if (server.DependencyServerId is not null)
            {
                var dependency = Servers.FirstOrDefault(
                    item =>
                        item.Id ==
                        server.DependencyServerId.Value);

                if (dependency is null)
                {
                    StatusMessage =
                        $"{server.ServerName}의 선행 서버를 찾을 수 없습니다.";

                    return;
                }

                if (dependency.Status != ServerStatus.Running)
                {
                    StatusMessage =
                        $"{dependency.ServerName} 선행 서버를 먼저 실행합니다.";

                    var dependencyStarted =
                        await StartServerCoreAsync(dependency);

                    if (!dependencyStarted)
                    {
                        StatusMessage =
                            $"{dependency.ServerName} 실행 실패로 " +
                            "전체 실행을 중단했습니다.";

                        return;
                    }
                }

                StatusMessage =
                    $"{dependency.ServerName} 서버의 정상 상태를 확인 중입니다.";

                var dependencyHealthy =
                    await WaitForServerHealthyAsync(
                        dependency,
                        TimeSpan.FromSeconds(15));

                if (!dependencyHealthy)
                {
                    StatusMessage =
                        $"{dependency.ServerName} 서버가 제한 시간 내 " +
                        "정상 상태가 되지 않아 전체 실행을 중단했습니다.";

                    return;
                }
            }

            StatusMessage =
                $"{server.ServerName} 서버를 실행하는 중입니다.";

            var success =
                await StartServerCoreAsync(server);

            if (!success)
            {
                StatusMessage =
                    $"{server.ServerName} 서버 실행 실패로 " +
                    "전체 실행을 중단했습니다.";

                return;
            }

            startedCount++;

            var healthy =
                await WaitForServerHealthyAsync(
                    server,
                    TimeSpan.FromSeconds(15));

            if (!healthy)
            {
                StatusMessage =
                    $"{server.ServerName} 서버가 실행됐지만 " +
                    "TCP 정상 상태가 되지 않았습니다.";

                return;
            }
        }

        StatusMessage =
            $"서버 {startedCount}개를 의존 순서에 따라 실행했습니다.";
    }

    private async Task StopAllServersAsync()
    {
        var targets = Servers
            .Where(server =>
                server.Status == ServerStatus.Running)
            .OrderByDescending(server => server.StartOrder)
            .ThenByDescending(server => server.ServerName)
            .ToList();

        if (targets.Count == 0)
        {
            StatusMessage = "종료할 서버가 없습니다.";
            return;
        }

        var result = WpfMessageBox.Show(
            $"실행 중인 서버 {targets.Count}개를 모두 종료하시겠습니까?",
            "전체 서버 종료",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var stoppedCount = 0;

        foreach (var server in targets)
        {
            StatusMessage =
                $"{server.ServerName} 서버를 종료하는 중입니다.";

            var success =
                await StopServerCoreAsync(server);

            if (success)
            {
                stoppedCount++;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(500));
        }

        StatusMessage =
            $"서버 {stoppedCount}개를 역순으로 종료했습니다.";
    }

    private void RefreshAvailableDependencyServers()
    {
        AvailableDependencyServers.Clear();

        foreach (var server in Servers)
        {
            if (SelectedServer is not null &&
                server.Id == SelectedServer.Id)
            {
                continue;
            }

            AvailableDependencyServers.Add(server);
        }
    }

    private bool HasCircularDependency(
        GameServerModel server,
        Guid? dependencyServerId)
    {
        if (dependencyServerId is null)
        {
            return false;
        }

        var visited = new HashSet<Guid>();
        var currentId = dependencyServerId;

        while (currentId is not null)
        {
            if (currentId == server.Id)
            {
                return true;
            }

            if (!visited.Add(currentId.Value))
            {
                return true;
            }

            var currentServer = Servers.FirstOrDefault(
                item => item.Id == currentId.Value);

            currentId =
                currentServer?.DependencyServerId;
        }

        return false;
    }

    private async Task<bool> WaitForServerHealthyAsync(
        GameServerModel server,
        TimeSpan timeout)
    {
        var startedAt = DateTime.Now;

        while (DateTime.Now - startedAt < timeout)
        {
            if (server.Status != ServerStatus.Running)
            {
                return false;
            }

            await CheckServerTcpAsync(
                server,
                CancellationToken.None);

            if (server.TcpStatus ==
                TcpConnectionStatus.Connected)
            {
                return true;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(1));
        }

        return false;
    }

    private bool CanSendServerCommand()
    {
        return SelectedServer is not null &&
               SelectedServer.Status ==
                   ServerStatus.Running;
    }

    private async Task SendServerCommandAsync()
    {
        if (SelectedServer is null)
        {
            StatusMessage =
                "명령을 전송할 서버를 선택하세요.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                ServerCommandInput))
        {
            StatusMessage =
                "전송할 서버 명령을 입력하세요.";

            return;
        }

        var server = SelectedServer;
        var command = ServerCommandInput.Trim();

        var isShutdownCommand =
            command.Equals(
                "shutdown",
                StringComparison.OrdinalIgnoreCase);

        try
        {
            if (isShutdownCommand)
            {
                _commandShutdownServers.Add(server.Id);

                server.Status = ServerStatus.Stopping;
                server.OverallStatus =
                    ServerOverallStatus.Stopped;

                RefreshServerCommandStates();
            }

            await _serverProcessService.SendCommandAsync(
                server,
                command);

            StatusMessage =
                $"{server.ServerName}에 명령을 전송했습니다: " +
                command;

            await AddHistoryAsync(
                server,
                "COMMAND",
                true,
                $"서버 명령 전송: {command}");

            ServerCommandInput = string.Empty;
        }
        catch (Exception ex)
        {
            if (isShutdownCommand)
            {
                _commandShutdownServers.Remove(server.Id);

                server.Status = _serverProcessService.IsRunning(server)
                    ? ServerStatus.Running
                    : ServerStatus.Stopped;

                UpdateOverallStatus(server);
                RefreshServerCommandStates();
            }

            StatusMessage =
                $"{server.ServerName} 명령 전송 실패: " +
                ex.Message;

            await AddHistoryAsync(
                server,
                "COMMAND",
                false,
                $"{command} - {ex.Message}");
        }
    }

    private void StartResourceMonitoring()
    {
        if (IsResourceMonitoring)
        {
            return;
        }

        _resourceMonitorCancellationTokenSource =
            new CancellationTokenSource();

        IsResourceMonitoring = true;

        _ = RunResourceMonitoringAsync(
            _resourceMonitorCancellationTokenSource.Token);
    }

    private async Task RunResourceMonitoringAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var timer =
                new PeriodicTimer(
                    TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(
                       cancellationToken))
            {
                await UpdateServerResourceUsageAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsResourceMonitoring = false;
        }
    }

    private async Task UpdateServerResourceUsageAsync()
    {
        foreach (var server in Servers)
        {
            if (server.Status != ServerStatus.Running)
            {
                ResetServerResourceUsage(server);
                continue;
            }

            var snapshot =
                _serverProcessService
                    .GetResourceSnapshot(server);

            if (snapshot is null)
            {
                continue;
            }

            server.CpuUsagePercent =
                snapshot.CpuUsagePercent;

            server.MemoryUsageBytes =
                snapshot.MemoryUsageBytes;

            server.UptimeText =
                FormatUptime(snapshot.Uptime);

            await CheckResourceWarningAsync(server);
        }
    }

    private static string FormatUptime(
        TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return
                $"{(int)uptime.TotalDays}일 " +
                $"{uptime:hh\\:mm\\:ss}";
        }

        return uptime.ToString(@"hh\:mm\:ss");
    }
    private void ResetServerResourceUsage(
        GameServerModel server)
    {
        server.CpuUsagePercent = 0;
        server.MemoryUsageBytes = 0;
        server.UptimeText = "-";

        server.ConsecutiveCpuWarnings = 0;
        server.ConsecutiveMemoryWarnings = 0;
        server.HasResourceWarning = false;
        server.ResourceWarningText = "-";

        _activeCpuWarnings.Remove(server.Id);
        _activeMemoryWarnings.Remove(server.Id);
    }

    private async Task CheckResourceWarningAsync(
        GameServerModel server)
    {
        var memoryUsageMb =
            server.MemoryUsageBytes /
            1024d /
            1024d;

        if (server.CpuUsagePercent >=
            server.CpuWarningThreshold)
        {
            server.ConsecutiveCpuWarnings++;
        }
        else
        {
            server.ConsecutiveCpuWarnings = 0;

            if (_activeCpuWarnings.Remove(server.Id))
            {
                await AddHistoryAsync(
                    server,
                    "CPU_RECOVERED",
                    true,
                    $"CPU 사용률 정상화: " +
                    $"{server.CpuUsagePercent:F1}%");
            }
        }

        if (memoryUsageMb >=
            server.MemoryWarningThresholdMb)
        {
            server.ConsecutiveMemoryWarnings++;
        }
        else
        {
            server.ConsecutiveMemoryWarnings = 0;

            if (_activeMemoryWarnings.Remove(server.Id))
            {
                await AddHistoryAsync(
                    server,
                    "MEMORY_RECOVERED",
                    true,
                    $"메모리 사용량 정상화: " +
                    $"{memoryUsageMb:F1} MB");
            }
        }

        if (server.ConsecutiveCpuWarnings >= 3 &&
            _activeCpuWarnings.Add(server.Id))
        {
            var message =
                $"{server.ServerName} CPU 사용률 " +
                $"{server.CpuUsagePercent:F1}% " +
                $"(기준 {server.CpuWarningThreshold:F1}%)";

            _trayIconService.ShowWarning(
                "CPU 사용률 경고",
                message);

            await AddHistoryAsync(
                server,
                "CPU_WARNING",
                false,
                message);
        }

        if (server.ConsecutiveMemoryWarnings >= 3 &&
            _activeMemoryWarnings.Add(server.Id))
        {
            var message =
                $"{server.ServerName} 메모리 사용량 " +
                $"{memoryUsageMb:F1} MB " +
                $"(기준 {server.MemoryWarningThresholdMb:F1} MB)";

            _trayIconService.ShowWarning(
                "메모리 사용량 경고",
                message);

            await AddHistoryAsync(
                server,
                "MEMORY_WARNING",
                false,
                message);
        }

        server.HasResourceWarning =
            _activeCpuWarnings.Contains(server.Id) ||
            _activeMemoryWarnings.Contains(server.Id);

        server.ResourceWarningText =
            BuildResourceWarningText(
                server,
                memoryUsageMb);

        RefreshDashboard();
    }

    private string BuildResourceWarningText(
        GameServerModel server,
        double memoryUsageMb)
    {
        var warnings = new List<string>();

        if (_activeCpuWarnings.Contains(server.Id))
        {
            warnings.Add(
                $"CPU {server.CpuUsagePercent:F1}%");
        }

        if (_activeMemoryWarnings.Contains(server.Id))
        {
            warnings.Add(
                $"메모리 {memoryUsageMb:F1} MB");
        }

        return warnings.Count == 0
            ? "-"
            : string.Join(", ", warnings);
    }

    public void ShowTrayMinimizedMessage()
    {
        _trayIconService.ShowInformation(
            "GameServerManager",
            "프로그램이 시스템 트레이에서 계속 실행됩니다.");
    }

    private void OnTrayExitRequested(
        object? sender,
        EventArgs e)
    {
        ExitRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void RefreshDashboard()
    {
        TotalServerCount =
            Servers.Count;

        RunningServerCount =
            Servers.Count(server =>
                server.Status == ServerStatus.Running);

        HealthyServerCount =
            Servers.Count(server =>
                server.OverallStatus ==
                    ServerOverallStatus.Healthy);

        ProblemServerCount =
            Servers.Count(server =>
                server.OverallStatus is
                    ServerOverallStatus.Unreachable or
                    ServerOverallStatus.Crashed);

        WarningServerCount =
            Servers.Count(server =>
                server.HasResourceWarning);
    }

    private async Task ApplyHistoryFilterAsync()
    {
        try
        {
            StatusMessage =
                "서버 이력을 조회하는 중입니다.";

            var eventType =
                HistoryEventFilter == "전체"
                    ? null
                    : HistoryEventFilter;

            bool? isSuccess =
                HistoryResultFilter switch
                {
                    "성공" => true,
                    "실패" => false,
                    _ => null
                };

            var histories =
                await _database.GetFilteredHistoriesAsync(
                    HistoryServerFilter?.Id,
                    eventType,
                    isSuccess,
                    500);

            ServerHistories.Clear();

            foreach (var history in histories)
            {
                ServerHistories.Add(history);
            }

            StatusMessage =
                $"조건에 맞는 서버 이력 " +
                $"{ServerHistories.Count}건을 불러왔습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"서버 이력 조회 실패: {ex.Message}";
        }
    }

    private async Task ResetHistoryFilterAsync()
    {
        HistoryServerFilter = null;
        HistoryEventFilter = "전체";
        HistoryResultFilter = "전체";

        await ApplyHistoryFilterAsync();
    }

    private async Task CleanupOldHistoriesAsync()
    {
        try
        {
            if (HistoryRetentionDays <= 0)
            {
                return;
            }

            var deletedCount =
                await _database.DeleteOldHistoriesAsync(
                    HistoryRetentionDays);

            if (deletedCount > 0)
            {
                StatusMessage =
                    $"{HistoryRetentionDays}일이 지난 서버 이력 " +
                    $"{deletedCount}건을 정리했습니다.";

                // await ApplyHistoryFilterAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"오래된 이력 정리 실패: {ex.Message}";
        }
    }

    private async Task SaveAppSettingsAsync()
    {
        try
        {
            await _appSettingsService.SaveAsync(
                _appSettings);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"프로그램 설정 저장 실패: {ex.Message}";
        }
    }

    private async Task LoadAppSettingsAsync()
    {
        _appSettings =
            await _appSettingsService.LoadAsync();

        _historyRetentionDays =
            _appSettings.HistoryRetentionDays;

        _startHealthMonitoringOnLaunch =
            _appSettings.StartHealthMonitoringOnLaunch;

        OnPropertyChanged(
            nameof(HistoryRetentionDays));

        OnPropertyChanged(
            nameof(StartHealthMonitoringOnLaunch));
    }

    private async Task SaveTcpHistoryIfChangedAsync(
        GameServerModel server)
    {
        var currentStatus =
            server.TcpStatus;

        if (currentStatus is
            TcpConnectionStatus.Checking or
            TcpConnectionStatus.NotChecked)
        {
            return;
        }

        if (_lastHistoryTcpStatuses.TryGetValue(
                server.Id,
                out var previousStatus) &&
            previousStatus == currentStatus)
        {
            return;
        }

        _lastHistoryTcpStatuses[server.Id] =
            currentStatus;

        if (currentStatus ==
            TcpConnectionStatus.Connected)
        {
            await AddHistoryAsync(
                server,
                "TCP_CHECK",
                true,
                $"TCP 연결 성공. 응답 시간: " +
                $"{server.ResponseTimeText}");

            return;
        }

        if (currentStatus ==
            TcpConnectionStatus.Failed)
        {
            await AddHistoryAsync(
                server,
                "TCP_CHECK",
                false,
                $"TCP 연결 실패. " +
                $"{server.Host}:{server.Port}");
        }
    }

    private bool MatchesCurrentHistoryFilter(
        ServerHistoryEntry history)
    {
        if (HistoryServerFilter is not null &&
            history.ServerId != HistoryServerFilter.Id)
        {
            return false;
        }

        if (HistoryEventFilter != "전체" &&
            history.EventType != HistoryEventFilter)
        {
            return false;
        }

        if (HistoryResultFilter == "성공" &&
            !history.IsSuccess)
        {
            return false;
        }

        if (HistoryResultFilter == "실패" &&
            history.IsSuccess)
        {
            return false;
        }

        return true;
    }

    private bool HasDuplicateEndpoint(
        string host,
        int port,
        Guid? excludeServerId = null)
    {
        return Servers.Any(server =>
            server.Id != excludeServerId &&
            string.Equals(
                server.Host,
                host,
                StringComparison.OrdinalIgnoreCase) &&
            server.Port == port);
    }
}