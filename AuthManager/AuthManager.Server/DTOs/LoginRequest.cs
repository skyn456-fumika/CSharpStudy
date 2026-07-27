using System.ComponentModel.DataAnnotations;

namespace AuthManager.Server.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "아이디를 입력해야 합니다.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "비밀번호를 입력해야 합니다.")]
    public string Password { get; set; } = "";
}