namespace RealtimeChat.Server.Models;

public class ChatMessageRecord
{
    public string Nickname { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; }
}