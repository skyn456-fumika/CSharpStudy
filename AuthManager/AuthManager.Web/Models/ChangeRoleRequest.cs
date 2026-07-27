using System.ComponentModel.DataAnnotations;

namespace AuthManager.Web.Models;

public class ChangeRoleRequest
{
    [Required]
    public string Role { get; set; } = "";
}