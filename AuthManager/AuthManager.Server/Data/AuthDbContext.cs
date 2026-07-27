using AuthManager.Server.Entities;
using Microsoft.EntityFrameworkCore;    

namespace AuthManager.Server.Data;

public class AuthDbContext : DbContext
{

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(user => user.Nickname)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(user => user.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(user => user.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(refreshToken => refreshToken.Id);

            entity.Property(refreshToken => refreshToken.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}