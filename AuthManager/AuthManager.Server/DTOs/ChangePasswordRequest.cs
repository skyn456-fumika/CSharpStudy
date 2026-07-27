using System.ComponentModel.DataAnnotations;

namespace AuthManager.Server.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "현재 비밀번호를 입력해야 합니다.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "새 비밀번호를 입력해야 합니다.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "새 비밀번호는 8자 이상이어야 합니다.")]
    public string NewPassword { get; set; } = "";
}