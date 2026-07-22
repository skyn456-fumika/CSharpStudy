namespace RealtimeChat.Server.Entities;

public class ChatMessageEntity
{
    public long Id { get; set; }

    public string Room { get; set; } = "";

    public string Nickname { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime SentAt { get; set; }
}