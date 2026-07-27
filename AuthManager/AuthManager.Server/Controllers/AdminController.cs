using AuthManager.Server.DTOs;
using AuthManager.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthManager.Server.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly IUserService userService;

    public AdminController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers()
    {
        List<AdminUserResponse> users =
            await userService.GetUsersAsync();

        return Ok(users);
    }

    [HttpPatch("users/{userId:long}/role")]
    public async Task<ActionResult<AdminUserResponse>> ChangeRole(
        long userId,
        [FromBody] ChangeRoleRequest request)
    {
        string? currentUserIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (long.TryParse(currentUserIdValue, out long currentUserId) &&
            currentUserId == userId)
        {
            return BadRequest(new
            {
                message = "자신의 권한은 변경할 수 없습니다."
            });
        }

        try
        {
            AdminUserResponse response =
                await userService.ChangeRoleAsync(
                    userId,
                    request.Role);

            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPatch("users/{userId:long}/status")]
    public async Task<ActionResult<AdminUserResponse>> ChangeStatus(
        long userId,
        [FromBody] ChangeUserStatusRequest request)
    {
        string? currentUserIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (long.TryParse(currentUserIdValue, out long currentUserId) &&
            currentUserId == userId)
        {
            return BadRequest(new
            {
                message = "자신의 계정 상태는 변경할 수 없습니다."
            });
        }

        try
        {
            AdminUserResponse response =
                await userService.ChangeStatusAsync(
                    userId,
                    request.IsActive);

            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }
}