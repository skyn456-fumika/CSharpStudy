using System.Security.Claims;
using AuthManager.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthManager.Server.Middleware;

public class UserStatusMiddleware
{
    private readonly RequestDelegate next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AuthDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        string? userIdValue = context.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!long.TryParse(userIdValue, out long userId))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "유효한 사용자 정보가 없습니다.",
                code = "INVALID_USER"
            });

            return;
        }

        bool? isActive = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (bool?)user.IsActive)
            .SingleOrDefaultAsync();

        if (isActive == null)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "사용자를 찾을 수 없습니다.",
                code = "USER_NOT_FOUND"
            });

            return;
        }

        if (!isActive.Value)
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "정지된 계정입니다.",
                code = "ACCOUNT_INACTIVE"
            });

            return;
        }

        await next(context);
    }
}