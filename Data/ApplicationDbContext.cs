using Microsoft.EntityFrameworkCore;
using ChatApp.Models.Entities;

namespace ChatApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatRoomParticipant> ChatRoomParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.ConnectionId);
        });

        // ChatRoom Configuration
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired();
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ChatRoomParticipant Configuration (Many-to-Many)
        modelBuilder.Entity<ChatRoomParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ChatRoom)
                  .WithMany(e => e.Participants)
                  .HasForeignKey(e => e.ChatRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.ChatRoomParticipants)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ChatRoomId, e.UserId }).IsUnique();
        });

        // Message Configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SentAt).IsRequired();
            entity.HasOne(e => e.SenderUser)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.SenderUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ChatRoom)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ChatRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ChatRoomId);
            entity.HasIndex(e => e.SentAt);
        });
    }
}
