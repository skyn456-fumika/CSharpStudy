namespace AuthManager.Server.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }

    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }

    public long UserId { get; set; }
    public string Username { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Role { get; set; } = "";
}