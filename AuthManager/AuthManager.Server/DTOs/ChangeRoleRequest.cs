using System.ComponentModel.DataAnnotations;

namespace AuthManager.Server.DTOs;

public class ChangeRoleRequest
{
    [Required(ErrorMessage = "역할을 입력해야 합니다.")]
    public string Role { get; set; } = "";
}