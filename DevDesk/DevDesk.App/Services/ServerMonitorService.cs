namespace DevDesk.App.Services;

public class ServerMonitorService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitorTask;

    public bool IsRunning =>
        _monitorTask is not null &&
        !_monitorTask.IsCompleted;

    public void Start(
        int intervalSeconds,
        Func<CancellationToken, Task> monitorAction)
    {
        if (IsRunning)
        {
            return;
        }

        if (intervalSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds),
                "감시 주기는 1초 이상이어야 합니다.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _monitorTask = Task.Run(
            () => RunAsync(intervalSeconds, monitorAction, token),
            token);
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource is null)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();

        if (_monitorTask is not null)
        {
            try
            {
                await _monitorTask;
            }
            catch (OperationCanceledException)
            {
                // 정상적인 감시 중지
            }
        }

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        _monitorTask = null;
    }

    private static async Task RunAsync(
        int intervalSeconds,
        Func<CancellationToken, Task> monitorAction,
        CancellationToken cancellationToken)
    {
        await monitorAction(cancellationToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await monitorAction(cancellationToken);
        }
    }
}