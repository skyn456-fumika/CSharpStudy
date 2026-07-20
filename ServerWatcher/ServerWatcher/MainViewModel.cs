using System.Collections.ObjectModel;

namespace ServerWatcher;


public class MainViewModel : ObservableObject
{
    private readonly ServerCheckService serverCheckService;
    private readonly ServerStorageService storageService;

    private string serverName = "";
    private string serverUrl = "";
    private ServerInfo? selectedServer;
    private CancellationTokenSource? monitoringCancellationTokenSource;
    private bool isMonitoring;
    private int monitoringIntervalSeconds = 10;

    public MainViewModel()
    {
        serverCheckService = new ServerCheckService();
        storageService = new ServerStorageService();

        AddServerCommand = new AsyncRelayCommand(
            AddServerAsync,
            CanAddServer);

        DeleteServerCommand = new AsyncRelayCommand(
            DeleteSelectedServerAsync,
            CanDeleteServer);

        CheckServerCommand = new AsyncRelayCommand(
            CheckSelectedServerAsync,
            CanCheckServer);

        CheckAllServersCommand = new AsyncRelayCommand(
            CheckAllServersAsync,
            CanCheckAllServers);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync,
            CanSave);

        StartMonitoringCommand = new AsyncRelayCommand(
            StartMonitoringAsync,
            CanStartMonitoring);

        StopMonitoringCommand = new RelayCommand(
            StopMonitoring,
            CanStopMonitoring);

        ClearHistoryCommand = new AsyncRelayCommand(
            ClearSelectedServerHistoryAsync,
            CanClearHistory);
    }

    public ObservableCollection<ServerInfo> Servers { get; }
        = new();

    public AsyncRelayCommand AddServerCommand { get; }

    public AsyncRelayCommand DeleteServerCommand { get; }

    public AsyncRelayCommand CheckServerCommand { get; }

    public AsyncRelayCommand CheckAllServersCommand { get; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand StartMonitoringCommand { get; }

    public RelayCommand StopMonitoringCommand { get; }

    public AsyncRelayCommand ClearHistoryCommand { get; }

    public string ServerName
    {
        get => serverName;

        set
        {
            if (SetProperty(ref serverName, value))
            {
                AddServerCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ServerUrl
    {
        get => serverUrl;

        set
        {
            if (SetProperty(ref serverUrl, value))
            {
                AddServerCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsMonitoring
    {
        get => isMonitoring;

        private set
        {
            if (SetProperty(ref isMonitoring, value))
            {
                StartMonitoringCommand.RaiseCanExecuteChanged();
                StopMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ServerInfo? SelectedServer
    {
        get => selectedServer;

        set
        {
            if (SetProperty(ref selectedServer, value))
            {
                DeleteServerCommand
                    .RaiseCanExecuteChanged();

                CheckServerCommand
                    .RaiseCanExecuteChanged();

                ClearHistoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int MonitoringIntervalSeconds
    {
        get => monitoringIntervalSeconds;

        set
        {
            if (SetProperty(
                    ref monitoringIntervalSeconds,
                    value))
            {
                StartMonitoringCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public void Stop()
    {
        monitoringCancellationTokenSource?.Cancel();
    }

    private bool CanAddServer()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            return false;
        }

        if (!Uri.TryCreate(
                ServerUrl,
                UriKind.Absolute,
                out Uri? uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps;
    }

    private async Task AddServerAsync()
    {
        Servers.Add(new ServerInfo
        {
            Name = ServerName.Trim(),
            Url = ServerUrl.Trim()
        });

        ServerName = "";
        ServerUrl = "";

        CheckAllServersCommand.RaiseCanExecuteChanged();
        StartMonitoringCommand.RaiseCanExecuteChanged();

        await storageService.SaveAsync(Servers);
    }

    private bool CanDeleteServer()
    {
        return SelectedServer != null;
    }

    private async Task DeleteSelectedServerAsync()
    {
        if (SelectedServer == null)
        {
            return;
        }

        Servers.Remove(SelectedServer);
        SelectedServer = null;

        CheckAllServersCommand.RaiseCanExecuteChanged();
        StartMonitoringCommand.RaiseCanExecuteChanged();

        await storageService.SaveAsync(Servers);
    }

    private bool CanCheckServer()
    {
        return SelectedServer != null
            && !SelectedServer.IsChecking;
    }

    private async Task CheckSelectedServerAsync()
    {
        if (SelectedServer == null)
        {
            return;
        }

        CheckServerCommand.RaiseCanExecuteChanged();

        try
        {
            await serverCheckService.CheckAsync(
                SelectedServer);

            await storageService.SaveAsync(
                Servers);
        }
        finally
        {
            CheckServerCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanCheckAllServers()
    {
        return Servers.Count > 0;
    }

    private async Task CheckAllServersAsync()
    {
        await CheckAllServersInternalAsync();
    }

    private async Task CheckAllServersInternalAsync()
    {
        List<Task> tasks = Servers
            .Select(server =>
                serverCheckService.CheckAsync(server))
            .ToList();

        await Task.WhenAll(tasks);

        await storageService.SaveAsync(
            Servers);
    }

    private bool CanSave()
    {
        return Servers.Count > 0;
    }

    private async Task SaveAsync()
    {
        await storageService.SaveAsync(Servers);
    }

    public async Task InitializeAsync()
    {
        List<ServerInfo> loadedServers =
            await storageService.LoadAsync();

        Servers.Clear();

        foreach (ServerInfo server in loadedServers)
        {
            Servers.Add(server);
        }

        CheckAllServersCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
    }

    private bool CanStartMonitoring()
    {
        return Servers.Count > 0
            && !IsMonitoring
            && MonitoringIntervalSeconds >= 1;
    }

    private bool CanStopMonitoring()
    {
        return IsMonitoring;
    }

    private async Task StartMonitoringAsync()
    {
        monitoringCancellationTokenSource?.Dispose();

        monitoringCancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            monitoringCancellationTokenSource.Token;

        IsMonitoring = true;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await CheckAllServersInternalAsync();

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        MonitoringIntervalSeconds),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 사용자가 중지한 정상적인 취소
        }
        finally
        {
            IsMonitoring = false;
        }
    }

    private void StopMonitoring()
    {
        monitoringCancellationTokenSource?.Cancel();
    }

    private bool CanClearHistory()
    {
        return SelectedServer != null && SelectedServer.Histories.Count > 0;
    }

    private async Task ClearSelectedServerHistoryAsync()
    {
        if (SelectedServer == null)
        {
            return;
        }

        SelectedServer.Histories.Clear();

        ClearHistoryCommand.RaiseCanExecuteChanged();

        await storageService.SaveAsync(Servers);
    }
}