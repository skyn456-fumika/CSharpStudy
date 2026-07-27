using System.ComponentModel.DataAnnotations;

namespace AuthManager.Server.DTOs;

public class UpdateNicknameRequest
{
    [Required(ErrorMessage = "닉네임을 입력해야 합니다.")]
    [StringLength(
        20,
        MinimumLength = 2,
        ErrorMessage = "닉네임은 2자 이상 20자 이하여야 합니다.")]
    public string Nickname { get; set; } = "";
}