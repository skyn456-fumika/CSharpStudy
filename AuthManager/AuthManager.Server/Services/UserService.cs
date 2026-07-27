using AuthManager.Server.Data;
using AuthManager.Server.DTOs;
using AuthManager.Server.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthManager.Server.Services;

public class UserService : IUserService
{
    private readonly AuthDbContext dbContext;
    private readonly IPasswordHasher<AppUser> passwordHasher;

    public UserService(
        AuthDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    public async Task<MyProfileResponse> GetMyProfileAsync(long userId)
    {
        AppUser? user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("사용자를 찾을 수 없습니다.");
        }

        return new MyProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<List<AdminUserResponse>> GetUsersAsync()
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Id)
            .Select(user => new AdminUserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AdminUserResponse> ChangeRoleAsync(
        long userId,
        string role)
    {
        string normalizedRole = role
            .Trim()
            .ToUpperInvariant();

        if (normalizedRole != "USER" &&
            normalizedRole != "ADMIN")
        {
            throw new ArgumentException(
                "역할은 USER 또는 ADMIN만 사용할 수 있습니다.");
        }

        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "사용자를 찾을 수 없습니다.");
        }

        user.Role = normalizedRole;

        await dbContext.SaveChangesAsync();

        return new AdminUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AdminUserResponse> ChangeStatusAsync(
        long userId,
        bool isActive)
    {
        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "사용자를 찾을 수 없습니다.");
        }

        user.IsActive = isActive;

        await dbContext.SaveChangesAsync();

        return new AdminUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request)
    {
        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "사용자를 찾을 수 없습니다.");
        }

        PasswordVerificationResult result =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "현재 비밀번호가 올바르지 않습니다.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ArgumentException(
                "새 비밀번호는 현재 비밀번호와 달라야 합니다.");
        }

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                request.NewPassword);

        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateNicknameAsync(
        long userId,
        UpdateNicknameRequest request)
    {
        AppUser? user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "사용자를 찾을 수 없습니다.");
        }

        string nickname = request.Nickname.Trim();

        if (user.Nickname == nickname)
        {
            throw new ArgumentException(
                "현재 닉네임과 동일합니다.");
        }

        bool nicknameExists =
            await dbContext.Users.AnyAsync(
                existingUser =>
                    existingUser.Id != userId &&
                    existingUser.Nickname == nickname);

        if (nicknameExists)
        {
            throw new InvalidOperationException(
                "이미 사용 중인 닉네임입니다.");
        }

        user.Nickname = nickname;

        await dbContext.SaveChangesAsync();
    }
}