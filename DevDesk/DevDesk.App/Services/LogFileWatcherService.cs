using System.IO;
using System.Text;

namespace DevDesk.App.Services;

public class LogFileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _readCancellationTokenSource;
    private long _lastPosition;
    private string? _filePath;
    private bool _isReading;

    public event EventHandler<string>? LineAdded;
    public event EventHandler<string>? WatcherError;

    public bool IsRunning => _watcher?.EnableRaisingEvents == true;

    public async Task<IReadOnlyList<string>> StartAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "감시할 로그 파일을 찾을 수 없습니다.",
                filePath);
        }

        await StopAsync();

        _filePath = filePath;
        _readCancellationTokenSource = new CancellationTokenSource();

        var existingLines = await ReadExistingLinesAsync(
            filePath,
            _readCancellationTokenSource.Token);

        _lastPosition = new FileInfo(filePath).Length;

        var directoryPath = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException(
                "로그 파일의 폴더 경로를 확인할 수 없습니다.");
        }

        _watcher = new FileSystemWatcher(directoryPath, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.Size
                           | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Error += OnWatcherError;

        return existingLines;
    }

    public Task StopAsync()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_readCancellationTokenSource is not null)
        {
            _readCancellationTokenSource.Cancel();
            _readCancellationTokenSource.Dispose();
            _readCancellationTokenSource = null;
        }

        _filePath = null;
        _lastPosition = 0;

        return Task.CompletedTask;
    }

    private static async Task<IReadOnlyList<string>> ReadExistingLinesAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is not null)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        await ReadAddedLinesAsync();
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        WatcherError?.Invoke(
            this,
            $"로그 파일 이름이 변경되었습니다: {e.Name}");

        await StopAsync();
    }

    private async void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        WatcherError?.Invoke(
            this,
            "감시 중인 로그 파일이 삭제되었습니다.");

        await StopAsync();
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        WatcherError?.Invoke(
            this,
            $"로그 감시 오류: {e.GetException().Message}");
    }

    private async Task ReadAddedLinesAsync()
    {
        if (_isReading ||
            string.IsNullOrWhiteSpace(_filePath) ||
            _readCancellationTokenSource is null)
        {
            return;
        }

        _isReading = true;

        try
        {
            await Task.Delay(
                50,
                _readCancellationTokenSource.Token);

            if (!File.Exists(_filePath))
            {
                return;
            }

            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            if (stream.Length < _lastPosition)
            {
                _lastPosition = 0;
            }

            stream.Seek(_lastPosition, SeekOrigin.Begin);

            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(
                    _readCancellationTokenSource.Token);

                if (line is not null)
                {
                    LineAdded?.Invoke(this, line);
                }
            }

            _lastPosition = stream.Position;
        }
        catch (OperationCanceledException)
        {
            // 감시 중지 시 발생할 수 있는 정상 취소
        }
        catch (Exception ex)
        {
            WatcherError?.Invoke(
                this,
                $"로그 파일 읽기 실패: {ex.Message}");
        }
        finally
        {
            _isReading = false;
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}