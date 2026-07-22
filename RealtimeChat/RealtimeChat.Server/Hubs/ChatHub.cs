using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RealtimeChat.Server.Models;
using Microsoft.EntityFrameworkCore;
using RealtimeChat.Server.Data;
using RealtimeChat.Server.Entities;

namespace RealtimeChat.Server.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, ChatUser> Users = new();

    private readonly IDbContextFactory<ChatDbContext> dbContextFactory;

    public ChatHub(IDbContextFactory<ChatDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    private static readonly HashSet<string> AllowedRooms =
    [
        "general",
        "game",
        "free"
    ];

    public async Task Join(string nickname, string room)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new HubException("닉네임을 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(room) || !AllowedRooms.Contains(room))
        {
            throw new HubException("존재하지 않는 채팅방입니다.");
        }

        string trimmedNickname = nickname.Trim();

        bool duplicated = Users.Any(user =>
            user.Key != Context.ConnectionId &&
            user.Value.Room == room &&
            user.Value.Nickname.Equals(trimmedNickname, StringComparison.OrdinalIgnoreCase));

        if (duplicated)
        {
            throw new HubException("해당 채팅방에서 이미 사용 중인 닉네임입니다.");
        }

        if (Users.TryGetValue(Context.ConnectionId, out ChatUser? previousUser))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousUser.Room);

            await Clients.Group(previousUser.Room).SendAsync(
                "ReceiveSystemMessage",
                $"{previousUser.Nickname}님이 퇴장했습니다.");

            Users.TryRemove(Context.ConnectionId, out _);

            await BroadcastUsersAsync(previousUser.Room);
        }

        ChatUser chatUser = new()
        {
            Nickname = trimmedNickname,
            Room = room
        };

        Users[Context.ConnectionId] = chatUser;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            room);

        await SendRoomHistoryAsync(room);

        await Clients.Group(room).SendAsync(
            "ReceiveSystemMessage",
            $"{trimmedNickname}님이 입장했습니다.");

        await BroadcastUsersAsync(room);
    }

    public async Task SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!Users.TryGetValue(Context.ConnectionId, out ChatUser? user))
        {
            throw new HubException("채팅방에 먼저 입장해야 합니다.");
        }

        string trimmedMessage = message.Trim();

        if (trimmedMessage.Length > 1000)
        {
            throw new HubException("메시지는 1000자를 초과할 수 없습니다.");
        }

        ChatMessageEntity chatMessage = new()
        {
            Room = user.Room,
            Nickname = user.Nickname,
            Message = trimmedMessage,
            SentAt = DateTime.UtcNow
        };

        await using ChatDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync();

        dbContext.ChatMessages.Add(chatMessage);
        await dbContext.SaveChangesAsync();

        await Clients.Group(user.Room).SendAsync(
            "ReceiveMessage",
            chatMessage.Nickname,
            chatMessage.Message,
            chatMessage.SentAt);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Users.TryRemove(Context.ConnectionId, out ChatUser? user))
        {
            await Clients.Group(user.Room).SendAsync(
                "ReceiveSystemMessage",
                $"{user.Nickname}님이 퇴장했습니다.");

            await BroadcastUsersAsync(user.Room);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastUsersAsync(string room)
    {
        string[] nicknames = Users.Values
            .Where(user => user.Room == room)
            .Select(user => user.Nickname)
            .OrderBy(nickname => nickname)
            .ToArray();

        await Clients.Group(room).SendAsync(
            "ReceiveUsers",
            nicknames);
    }

    public async Task ChangeRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room) || !AllowedRooms.Contains(room))
        {
            throw new HubException("존재하지 않는 채팅방입니다.");
        }

        if (!Users.TryGetValue(Context.ConnectionId, out ChatUser? user))
        {
            throw new HubException("채팅방에 먼저 입장해야 합니다.");
        }

        if (user.Room == room)
        {
            throw new HubException("이미 현재 채팅방에 있습니다.");
        }

        bool duplicated = Users.Any(item =>
            item.Key != Context.ConnectionId &&
            item.Value.Room == room &&
            item.Value.Nickname.Equals(
                user.Nickname,
                StringComparison.OrdinalIgnoreCase));

        if (duplicated)
        {
            throw new HubException("이동할 채팅방에서 이미 사용 중인 닉네임입니다.");
        }

        string previousRoom = user.Room;

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            previousRoom);

        await Clients.Group(previousRoom).SendAsync(
            "ReceiveSystemMessage",
            $"{user.Nickname}님이 다른 채팅방으로 이동했습니다.");

        user.Room = room;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            room);

        await SendRoomHistoryAsync(room);

        await Clients.Group(room).SendAsync(
            "ReceiveSystemMessage",
            $"{user.Nickname}님이 입장했습니다.");

        await BroadcastUsersAsync(previousRoom);
        await BroadcastUsersAsync(room);
    }

    public async Task SendWhisper(string targetNickname, string message)
    {
        if (string.IsNullOrWhiteSpace(targetNickname) ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!Users.TryGetValue(Context.ConnectionId, out ChatUser? sender))
        {
            throw new HubException("채팅방에 먼저 입장해야 합니다.");
        }

        KeyValuePair<string, ChatUser> target = Users.FirstOrDefault(item =>
            item.Value.Room == sender.Room &&
            item.Value.Nickname.Equals(
                targetNickname.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(target.Key))
        {
            throw new HubException("현재 채팅방에 해당 사용자가 없습니다.");
        }

        string trimmedMessage = message.Trim();

        await Clients.Client(target.Key).SendAsync(
            "ReceiveWhisper",
            sender.Nickname,
            trimmedMessage,
            DateTime.UtcNow,
            false);

        if (target.Key != Context.ConnectionId)
        {
            await Clients.Caller.SendAsync(
                "ReceiveWhisper",
                target.Value.Nickname,
                trimmedMessage,
                DateTime.UtcNow,
                true);
        }
    }

    private async Task SendRoomHistoryAsync(string room)
    {
        await using ChatDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync();

        List<ChatMessageEntity> history = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.Room == room)
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Take(50)
            .ToListAsync();

        history.Reverse();

        ChatMessageRecord[] records = history
            .Select(message => new ChatMessageRecord
            {
                Nickname = message.Nickname,
                Message = message.Message,
                SentAt = message.SentAt
            })
            .ToArray();

        await Clients.Caller.SendAsync(
            "ReceiveRoomHistory",
            records);
    }
}