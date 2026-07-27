using System.Security.Claims;
using AuthManager.Server.DTOs;
using AuthManager.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthManager.Server.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MyProfileResponse>> GetMyProfile()
    {
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdValue, out long userId))
        {
            return Unauthorized(new
            {
                message = "유효한 사용자 정보가 없습니다."
            });
        }

        try
        {
            MyProfileResponse response =
                await userService.GetMyProfileAsync(userId);

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

    [Authorize]
    [HttpPatch("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        string? userIdValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

        if (!long.TryParse(userIdValue, out long userId))
        {
            return Unauthorized(new
            {
                message = "유효한 사용자 정보가 없습니다."
            });
        }

        try
        {
            await userService.ChangePasswordAsync(
                userId,
                request);

            return NoContent();
        }
        catch (UnauthorizedAccessException exception)
        {
            return BadRequest(new
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
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    [Authorize]
    [HttpPatch("me/nickname")]
    public async Task<IActionResult> UpdateNickname(
        [FromBody] UpdateNicknameRequest request)
    {
        string? userIdValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(userIdValue, out long userId))
        {
            return Unauthorized(new
            {
                message = "유효한 사용자 정보가 없습니다."
            });
        }

        try
        {
            await userService.UpdateNicknameAsync(
                userId,
                request);

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
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
