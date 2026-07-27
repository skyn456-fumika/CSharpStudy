using AuthManager.Server.Data;
using AuthManager.Server.DTOs;
using AuthManager.Server.Entities;
using AuthManager.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthManager.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly AuthDbContext dbContext;

    public AuthController(
        IAuthService authService,
        AuthDbContext dbContext)
    {
        this.authService = authService;
        this.dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request)
    {
        try
        {
            RegisterResponse response =
                await authService.RegisterAsync(request);

            return Created(
                $"/api/users/{response.Id}",
                response);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request)
    {
        try
        {
            LoginResponse response =
                await authService.LoginAsync(request);

            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshTokenRequest request)
    {
        try
        {
            LoginResponse response =
                await authService.RefreshAsync(request);

            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new
            {
                message = exception.Message,
                code = "REFRESH_TOKEN_INVALID"
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request)
    {
        await authService.LogoutAsync(request);

        return NoContent();
    }
}