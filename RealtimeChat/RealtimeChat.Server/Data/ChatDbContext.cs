using Microsoft.EntityFrameworkCore;
using RealtimeChat.Server.Entities;

namespace RealtimeChat.Server.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.ToTable("ChatMessages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Room)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(message => message.Nickname)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(message => message.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(message => message.SentAt)
                .IsRequired();

            entity.HasIndex(message => new
            {
                message.Room,
                message.SentAt
            });
        });
    }
}