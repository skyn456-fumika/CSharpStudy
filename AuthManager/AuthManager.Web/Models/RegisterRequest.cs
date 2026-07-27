using System.ComponentModel.DataAnnotations;

namespace AuthManager.Web.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "아이디를 입력해야 합니다.")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "아이디는 4~50자여야 합니다.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "비밀번호를 입력해야 합니다.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "비밀번호는 8자 이상이어야 합니다.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "닉네임을 입력해야 합니다.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "닉네임은 2~50자여야 합니다.")]
    public string Nickname { get; set; } = "";
}