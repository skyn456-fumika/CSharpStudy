using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameServerManager.App.Models;

public class GameServerModel : INotifyPropertyChanged
{
    private string _serverName = string.Empty;
    private string _executablePath = string.Empty;
    private string _arguments = string.Empty;
    private string _workingDirectory = string.Empty;
    private string _host = "127.0.0.1";
    private int _port;
    private bool _autoRestart;
    private int? _processId;
    private ServerStatus _status = ServerStatus.Stopped;

    private TcpConnectionStatus _tcpStatus =
        TcpConnectionStatus.NotChecked;

    private long? _responseTimeMs;
    private DateTime? _lastCheckedAt;

    private ServerOverallStatus _overallStatus =
    ServerOverallStatus.Stopped;

    private int _consecutiveTcpFailures;
    private DateTime? _lastRestartedAt;

    private int _startOrder;

    private Guid? _dependencyServerId;
    private string _dependencyServerName = "없음";

    private double _cpuUsagePercent;
    private long _memoryUsageBytes;
    private string _uptimeText = "-";

    public string CpuUsageText =>
    Status == ServerStatus.Running
        ? $"{CpuUsagePercent:F1}%"
        : "-";

    public string MemoryUsageText =>
        Status == ServerStatus.Running
            ? $"{MemoryUsageBytes / 1024d / 1024d:F1} MB"
            : "-";

    private double _cpuWarningThreshold = 80;
    private double _memoryWarningThresholdMb = 500;

    private int _consecutiveCpuWarnings;
    private int _consecutiveMemoryWarnings;

    private bool _hasResourceWarning;
    private string _resourceWarningText = "-";

    public Guid Id { get; set; } = Guid.NewGuid();

    public string ServerName
    {
        get => _serverName;
        set => SetProperty(ref _serverName, value);
    }

    public string ExecutablePath
    {
        get => _executablePath;
        set => SetProperty(ref _executablePath, value);
    }

    public string Arguments
    {
        get => _arguments;
        set => SetProperty(ref _arguments, value);
    }

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => SetProperty(ref _workingDirectory, value);
    }

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public bool AutoRestart
    {
        get => _autoRestart;
        set => SetProperty(ref _autoRestart, value);
    }

    public int? ProcessId
    {
        get => _processId;
        set
        {
            if (SetProperty(ref _processId, value))
            {
                OnPropertyChanged(nameof(ProcessIdText));
            }
        }
    }

    public ServerStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CpuUsageText));
                OnPropertyChanged(nameof(MemoryUsageText));
            }
        }
    }

    public TcpConnectionStatus TcpStatus
    {
        get => _tcpStatus;
        set
        {
            if (SetProperty(ref _tcpStatus, value))
            {
                OnPropertyChanged(nameof(TcpStatusText));
            }
        }
    }

    public long? ResponseTimeMs
    {
        get => _responseTimeMs;
        set
        {
            if (SetProperty(ref _responseTimeMs, value))
            {
                OnPropertyChanged(nameof(ResponseTimeText));
            }
        }
    }

    public DateTime? LastCheckedAt
    {
        get => _lastCheckedAt;
        set
        {
            if (SetProperty(ref _lastCheckedAt, value))
            {
                OnPropertyChanged(nameof(LastCheckedAtText));
            }
        }
    }

    public int StartOrder
    {
        get => _startOrder;
        set => SetProperty(ref _startOrder, value);
    }

    public Guid? DependencyServerId
    {
        get => _dependencyServerId;
        set
        {
            if (SetProperty(ref _dependencyServerId, value))
            {
                OnPropertyChanged(nameof(DependencyServerName));
            }
        }
    }

    public string DependencyServerName
    {
        get => _dependencyServerName;
        set => SetProperty(ref _dependencyServerName, value);
    }

    public double CpuWarningThreshold
    {
        get => _cpuWarningThreshold;
        set => SetProperty(ref _cpuWarningThreshold, value);
    }

    public double MemoryWarningThresholdMb
    {
        get => _memoryWarningThresholdMb;
        set => SetProperty(ref _memoryWarningThresholdMb, value);
    }

    public int ConsecutiveCpuWarnings
    {
        get => _consecutiveCpuWarnings;
        set => SetProperty(ref _consecutiveCpuWarnings, value);
    }

    public int ConsecutiveMemoryWarnings
    {
        get => _consecutiveMemoryWarnings;
        set => SetProperty(ref _consecutiveMemoryWarnings, value);
    }

    public bool HasResourceWarning
    {
        get => _hasResourceWarning;
        set => SetProperty(ref _hasResourceWarning, value);
    }

    public string ResourceWarningText
    {
        get => _resourceWarningText;
        set => SetProperty(ref _resourceWarningText, value);
    }

    public string TcpStatusText =>
        TcpStatus switch
        {
            TcpConnectionStatus.Checking => "검사 중",
            TcpConnectionStatus.Connected => "연결 성공",
            TcpConnectionStatus.Failed => "연결 실패",
            _ => "검사 전"
        };

    public string ResponseTimeText =>
        ResponseTimeMs is null
            ? "-"
            : $"{ResponseTimeMs} ms";

    public string LastCheckedAtText =>
        LastCheckedAt?.ToString("HH:mm:ss") ?? "-";

    public string ProcessIdText =>
        ProcessId?.ToString() ?? "-";

    public string StatusText =>
        Status switch
        {
            ServerStatus.Starting => "시작 중",
            ServerStatus.Running => "실행 중",
            ServerStatus.Stopping => "종료 중",
            ServerStatus.Crashed => "비정상 종료",
            ServerStatus.ConnectionFailed => "연결 실패",
            _ => "중지"
        };

    public event PropertyChangedEventHandler? PropertyChanged;

    public ServerOverallStatus OverallStatus
    {
        get => _overallStatus;
        set
        {
            if (SetProperty(ref _overallStatus, value))
            {
                OnPropertyChanged(nameof(OverallStatusText));
            }
        }
    }

    public int ConsecutiveTcpFailures
    {
        get => _consecutiveTcpFailures;
        set
        {
            if (SetProperty(ref _consecutiveTcpFailures, value))
            {
                OnPropertyChanged(nameof(TcpFailureCountText));
            }
        }
    }

    public DateTime? LastRestartedAt
    {
        get => _lastRestartedAt;
        set
        {
            if (SetProperty(ref _lastRestartedAt, value))
            {
                OnPropertyChanged(nameof(LastRestartedAtText));
            }
        }
    }

    public string OverallStatusText =>
        OverallStatus switch
        {
            ServerOverallStatus.Starting => "시작 중",
            ServerOverallStatus.Healthy => "정상",
            ServerOverallStatus.ProcessOnly => "포트 대기",
            ServerOverallStatus.Unreachable => "연결 장애",
            ServerOverallStatus.Restarting => "재시작 중",
            ServerOverallStatus.Crashed => "비정상 종료",
            _ => "중지"
        };

    public string TcpFailureCountText =>
        ConsecutiveTcpFailures == 0
            ? "-"
            : $"{ConsecutiveTcpFailures}회";

    public string LastRestartedAtText =>
        LastRestartedAt?.ToString("HH:mm:ss") ?? "-";

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        set
        {
            if (SetProperty(ref _cpuUsagePercent, value))
            {
                OnPropertyChanged(nameof(CpuUsageText));
            }
        }
    }

    public long MemoryUsageBytes
    {
        get => _memoryUsageBytes;
        set
        {
            if (SetProperty(ref _memoryUsageBytes, value))
            {
                OnPropertyChanged(nameof(MemoryUsageText));
            }
        }
    }

    public string UptimeText
    {
        get => _uptimeText;
        set => SetProperty(ref _uptimeText, value);
    }

    protected virtual void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }
}