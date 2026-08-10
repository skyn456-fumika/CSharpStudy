using System.Net;
using System.Net.Sockets;
using System.Text;

var port = 7200;

if (args.Length > 0 &&
    int.TryParse(args[0], out var inputPort) &&
    inputPort is >= 1 and <= 65535)
{
    port = inputPort;
}

var listener = new TcpListener(
    IPAddress.Loopback,
    port);

using var cancellationTokenSource =
    new CancellationTokenSource();

var startedAt = DateTime.Now;
var connectedCount = 0;

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    listener.Start();

    Console.Title = $"TestGameServer - {port}";

    WriteLog(
        "INFO",
        $"테스트 게임 서버 시작");

    WriteLog(
        "INFO",
        $"127.0.0.1:{port} 연결 대기 중");

    var acceptTask = AcceptClientsAsync(
        listener,
        cancellationTokenSource.Token,
        () => Interlocked.Increment(
            ref connectedCount));

    var commandTask = ReadCommandsAsync(
        port,
        startedAt,
        () => connectedCount,
        cancellationTokenSource);

    await Task.WhenAny(
        acceptTask,
        commandTask);

    cancellationTokenSource.Cancel();

    try
    {
        await Task.WhenAll(
            acceptTask,
            commandTask);
    }
    catch (OperationCanceledException)
    {
        // 정상 종료
    }
}
catch (SocketException ex)
{
    WriteLog(
        "ERROR",
        $"서버 시작 실패: {ex.Message}");

    Environment.ExitCode = 1;
}
finally
{
    listener.Stop();

    WriteLog(
        "INFO",
        "서버 종료");
}

static async Task AcceptClientsAsync(
    TcpListener listener,
    CancellationToken cancellationToken,
    Action onConnected)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        TcpClient client;

        try
        {
            client =
                await listener.AcceptTcpClientAsync(
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        onConnected();

        _ = HandleClientAsync(client);
    }
}

static async Task HandleClientAsync(
    TcpClient client)
{
    using (client)
    {
        var remoteEndPoint =
            client.Client.RemoteEndPoint?.ToString()
            ?? "알 수 없음";

        WriteLog(
            "INFO",
            $"클라이언트 연결: {remoteEndPoint}");

        var response =
            Encoding.UTF8.GetBytes(
                "TestGameServer OK\n");

        var stream = client.GetStream();

        await stream.WriteAsync(response);
        await stream.FlushAsync();

        WriteLog(
            "INFO",
            $"클라이언트 연결 종료: {remoteEndPoint}");
    }
}

static async Task ReadCommandsAsync(
    int port,
    DateTime startedAt,
    Func<int> getConnectedCount,
    CancellationTokenSource cancellationTokenSource)
{
    while (!cancellationTokenSource.IsCancellationRequested)
    {
        var commandLine =
            await Console.In.ReadLineAsync();

        if (commandLine is null)
        {
            break;
        }

        var command = commandLine.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            continue;
        }

        WriteLog(
            "COMMAND",
            $"> {command}");

        if (command.Equals(
                "status",
                StringComparison.OrdinalIgnoreCase))
        {
            var uptime =
                DateTime.Now - startedAt;

            WriteLog(
                "INFO",
                $"상태: Running, " +
                $"포트: {port}, " +
                $"가동 시간: {uptime:dd\\.hh\\:mm\\:ss}");

            continue;
        }

        if (command.Equals(
                "players",
                StringComparison.OrdinalIgnoreCase))
        {
            WriteLog(
                "INFO",
                $"누적 TCP 연결 수: {getConnectedCount()}");

            continue;
        }

        if (command.StartsWith(
                "announce ",
                StringComparison.OrdinalIgnoreCase))
        {
            var message =
                command["announce ".Length..].Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                WriteLog(
                    "WARN",
                    "공지 내용을 입력하세요.");
            }
            else
            {
                WriteLog(
                    "NOTICE",
                    $"서버 공지: {message}");
            }

            continue;
        }

        if (command.Equals(
                "help",
                StringComparison.OrdinalIgnoreCase))
        {
            WriteLog(
                "INFO",
                "사용 가능한 명령: " +
                "status, players, announce <내용>, " +
                "shutdown, crash, help");

            continue;
        }

        if (command.Equals(
                "shutdown",
                StringComparison.OrdinalIgnoreCase))
        {
            WriteLog(
                "INFO",
                "관리자 명령으로 서버를 정상 종료합니다.");

            cancellationTokenSource.Cancel();
            break;
        }

        if (command.Equals(
                "crash",
                StringComparison.OrdinalIgnoreCase))
        {
            WriteLog(
                "ERROR",
                "테스트용 비정상 종료를 발생시킵니다.");

            Environment.Exit(100);
        }

        WriteLog(
            "WARN",
            $"알 수 없는 명령입니다: {command}");
    }
}

static void WriteLog(
    string level,
    string message)
{
    Console.WriteLine(
        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
        $"[{level}] {message}");
}