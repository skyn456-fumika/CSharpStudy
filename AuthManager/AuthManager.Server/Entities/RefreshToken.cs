namespace AuthManager.Server.Entities;

public class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string TokenHash { get; set; } = "";

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired =>
        DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked =>
        RevokedAt != null;

    public bool IsActive =>
        !IsExpired && !IsRevoked;
}