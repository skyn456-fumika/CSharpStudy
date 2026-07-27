using System.ComponentModel.DataAnnotations;

namespace AuthManager.Server.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh Token이 필요합니다.")]
    public string RefreshToken { get; set; } = "";
}