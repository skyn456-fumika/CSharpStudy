using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatServer;

internal class Program
{
    private const int Port = 5000;

    private static readonly ConcurrentDictionary<int, ClientConnection> Clients = new();
    private static int nextClientId;
    private static bool isShuttingDown;

    private static async Task Main()
    {
        TcpListener listener = new(IPAddress.Any, Port);
        listener.Start();

        Console.WriteLine($"채팅 서버가 {Port} 포트에서 시작되었습니다.");
        Console.WriteLine("서버 종료: exit");

        Task acceptTask = AcceptClientsAsync(listener);

        while (true)
        {
            string? command = Console.ReadLine();

            if (command?.Equals(
                "exit",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                break;
            }
        }

        isShuttingDown = true;
        listener.Stop();

        await BroadcastAsync("[알림] 서버가 종료됩니다.");

        foreach (ClientConnection client in Clients.Values)
        {
            client.Dispose();
        }

        Clients.Clear();

        try
        {
            await acceptTask;
        }
        catch (SocketException)
        {
            // listener.Stop()으로 접속 대기가 종료된 경우
        }
        catch (ObjectDisposedException)
        {
            // 정상 종료 과정
        }

        Console.WriteLine("서버가 종료되었습니다.");
    }

    private static async Task HandleClientAsync(ClientConnection client)
    {
        try
        {
            while (true)
            {
                string? nickname = await client.Reader.ReadLineAsync();

                if (nickname == null)
                {
                    return;
                }

                nickname = nickname.Trim();

                if (string.IsNullOrWhiteSpace(nickname))
                {
                    await client.Writer.WriteLineAsync("NICKNAME_INVALID");
                    continue;
                }

                if (IsNicknameInUse(nickname))
                {
                    await client.Writer.WriteLineAsync("NICKNAME_DUPLICATED");
                    continue;
                }

                client.Nickname = nickname;

                await client.Writer.WriteLineAsync("NICKNAME_ACCEPTED");

                client.IsReady = true;

                await BroadcastAsync(
                    $"[알림] {client.Nickname}님이 입장했습니다.");

                break;
            }

            while (true)
            {
                string? message = await client.Reader.ReadLineAsync();

                if (message == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (message.Equals(
                    "/users",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string[] nicknames = Clients.Values
                        .Where(connection =>
                            connection.IsReady &&
                            !string.IsNullOrWhiteSpace(connection.Nickname))
                        .Select(connection => connection.Nickname)
                        .OrderBy(nickname => nickname)
                        .ToArray();

                    string userList =
                        $"[접속자 {nicknames.Length}명] " +
                        string.Join(", ", nicknames);

                    await client.Writer.WriteLineAsync(userList);
                    continue;
                }

                if (message.StartsWith(
                    "/w ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await SendWhisperAsync(client, message);
                    continue;
                }

                Console.WriteLine($"{client.Nickname}: {message}");

                await BroadcastAsync(
                    $"{client.Nickname}: {message}");
            }
        }
        catch (IOException)
        {
            Console.WriteLine(
                $"{client.Nickname}의 연결이 끊어졌습니다.");
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"클라이언트 처리 오류: {exception.Message}");
        }
        finally
        {
            Clients.TryRemove(client.Id, out _);
            client.Dispose();

            if (!string.IsNullOrWhiteSpace(client.Nickname))
            {
                Console.WriteLine($"{client.Nickname} 연결 종료");

                if (!isShuttingDown)
                {
                    await BroadcastAsync(
                        $"[알림] {client.Nickname}님이 퇴장했습니다.");
                }
            }
        }
    }

    private static async Task BroadcastAsync(string message)
    {
        ClientConnection[] clients = Clients.Values
            .Where(client => client.IsReady)
            .ToArray();

        foreach (ClientConnection client in clients)
        {
            try
            {
                await client.Writer.WriteLineAsync(message);
            }
            catch
            {
                // 전송 실패한 클라이언트는
                // HandleClientAsync의 종료 과정에서 제거된다.
            }
        }
    }

    private static bool IsNicknameInUse(string nickname)
    {
        return Clients.Values.Any(client =>
            client.Nickname.Equals(
                nickname,
                StringComparison.OrdinalIgnoreCase));
    }

    private static async Task SendWhisperAsync(
        ClientConnection sender,
        string command)
    {
        string[] parts = command.Split(
            ' ',
            3,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            await sender.Writer.WriteLineAsync(
                "[안내] 사용법: /w 닉네임 메시지");

            return;
        }

        string targetNickname = parts[1];
        string whisperMessage = parts[2];

        ClientConnection? target = Clients.Values
            .FirstOrDefault(client =>
                client.IsReady &&
                client.Nickname.Equals(
                    targetNickname,
                    StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            await sender.Writer.WriteLineAsync(
                $"[안내] {targetNickname} 사용자를 찾을 수 없습니다.");

            return;
        }

        await target.Writer.WriteLineAsync(
            $"[귓속말] {sender.Nickname}: {whisperMessage}");

        if (target.Id != sender.Id)
        {
            await sender.Writer.WriteLineAsync(
                $"[귓속말 → {target.Nickname}] {whisperMessage}");
        }
    }

    private static async Task AcceptClientsAsync(TcpListener listener)
    {
        while (true)
        {
            TcpClient tcpClient = await listener.AcceptTcpClientAsync();
            int clientId = Interlocked.Increment(ref nextClientId);

            ClientConnection connection = new(clientId, tcpClient);
            Clients.TryAdd(clientId, connection);

            Console.WriteLine(
                $"클라이언트 {clientId} 접속: {tcpClient.Client.RemoteEndPoint}");

            _ = HandleClientAsync(connection);
        }
    }
}

internal sealed class ClientConnection : IDisposable
{
    public string Nickname { get; set; } = "";
    public bool IsReady { get; set; }

    public ClientConnection(int id, TcpClient tcpClient)
    {
        Id = id;
        TcpClient = tcpClient;

        NetworkStream stream = tcpClient.GetStream();

        Reader = new StreamReader(
            stream,
            Encoding.UTF8,
            leaveOpen: true);

        Writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            leaveOpen: true)
        {
            AutoFlush = true
        };
    }

    public int Id { get; }

    public TcpClient TcpClient { get; }

    public StreamReader Reader { get; }

    public StreamWriter Writer { get; }

    public void Dispose()
    {
        Reader.Dispose();
        Writer.Dispose();
        TcpClient.Dispose();
    }
}