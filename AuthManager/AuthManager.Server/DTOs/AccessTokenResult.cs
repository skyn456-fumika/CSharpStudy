namespace AuthManager.Server.DTOs;

public class AccessTokenResult
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}