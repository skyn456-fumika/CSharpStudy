namespace AuthManager.Web.Models;

public class RegisterResponse
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}