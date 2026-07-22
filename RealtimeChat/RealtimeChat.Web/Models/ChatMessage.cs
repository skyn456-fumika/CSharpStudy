namespace RealtimeChat.Web.Models;

public class ChatMessage
{
    public string Nickname { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool IsSystem { get; set; }
    public bool IsWhisper { get; set; }
    public bool IsSentWhisper { get; set; }
}