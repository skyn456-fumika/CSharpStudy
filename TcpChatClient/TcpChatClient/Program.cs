using System.Net.Sockets;
using System.Text;

namespace TcpChatClient;

internal class Program
{
    private const string ServerIp = "127.0.0.1";
    private const int ServerPort = 5000;

    private static async Task Main()
    {
        using TcpClient client = new();

        try
        {
            await client.ConnectAsync(ServerIp, ServerPort);

            Console.WriteLine("서버에 연결되었습니다.");
            Console.WriteLine("종료하려면 exit를 입력하세요.");

            using NetworkStream stream = client.GetStream();

            using StreamReader reader = new(
                stream,
                Encoding.UTF8,
                leaveOpen: true);

            using StreamWriter writer = new(
                stream,
                new UTF8Encoding(false),
                leaveOpen: true)
            {
                AutoFlush = true
            };

            while (true)
            {
                Console.Write("닉네임 입력: ");
                string? nickname = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(nickname))
                {
                    Console.WriteLine("닉네임을 입력해야 합니다.");
                    continue;
                }

                await writer.WriteLineAsync(nickname.Trim());

                string? response = await reader.ReadLineAsync();

                if (response == "NICKNAME_ACCEPTED")
                {
                    break;
                }

                if (response == "NICKNAME_DUPLICATED")
                {
                    Console.WriteLine("이미 사용 중인 닉네임입니다.");
                    continue;
                }

                Console.WriteLine("사용할 수 없는 닉네임입니다.");
            }

            Console.WriteLine("채팅을 시작합니다.");
            Console.WriteLine("접속자 목록: /users");
            Console.WriteLine("귓속말: /w 닉네임 메시지");
            Console.WriteLine("종료: exit");

            Task receiveTask = ReceiveMessagesAsync(reader);

            while (true)
            {
                string? message = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (message.Equals(
                    "exit",
                    StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await writer.WriteLineAsync(message);
            }

            client.Close();
            await receiveTask;
        }
        catch (SocketException exception)
        {
            Console.WriteLine($"서버 연결 실패: {exception.Message}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"오류 발생: {exception.Message}");
        }

        Console.WriteLine("클라이언트를 종료합니다.");
    }

    private static async Task ReceiveMessagesAsync(
        StreamReader reader)
    {
        try
        {
            while (true)
            {
                string? message = await reader.ReadLineAsync();

                if (message == null)
                {
                    Console.WriteLine("서버 연결이 종료되었습니다.");
                    break;
                }

                Console.WriteLine(message);
            }
        }
        catch (IOException)
        {
            Console.WriteLine("서버와의 연결이 끊어졌습니다.");
        }
        catch (ObjectDisposedException)
        {
            // 사용자가 exit로 정상 종료한 경우
        }
    }
}