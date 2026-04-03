using COEPD.SalesFunnelSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace COEPD.SalesFunnelSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<DemoBooking> DemoBookings => Set<DemoBooking>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<EmailAutomationLog> EmailAutomationLogs => Set<EmailAutomationLog>();
    public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs => Set<WhatsAppMessageLog>();
    public DbSet<LeadActivityLog> LeadActivityLogs => Set<LeadActivityLog>();
    public DbSet<FunnelEvent> FunnelEvents => Set<FunnelEvent>();
    public DbSet<LeadFollowUpJob> LeadFollowUpJobs => Set<LeadFollowUpJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Domain).HasColumnName("InterestedDomain").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("New").IsRequired();
            entity.Property(x => x.Score).HasMaxLength(20).HasDefaultValue("Warm").IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.FunnelStage).HasMaxLength(30).HasDefaultValue("New").IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Phone).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Source);
            entity.HasIndex(x => x.Domain);
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<DemoBooking>(entity =>
        {
            entity.Property(x => x.Day).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Slot).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.HasOne(x => x.Lead).WithMany(x => x.DemoBookings).HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(x => x.SessionId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Stage).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.SessionId).IsUnique();
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.Sender).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.HasOne(x => x.ChatSession).WithMany(x => x.Messages).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FailedLoginAttempts).HasDefaultValue(0).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<LeadActivityLog>(entity =>
        {
            entity.Property(x => x.ActivityType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.LeadId);
        });

        modelBuilder.Entity<FunnelEvent>(entity =>
        {
            entity.Property(x => x.Stage).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Timestamp).IsRequired();
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.Stage);
        });

        modelBuilder.Entity<LeadFollowUpJob>(entity =>
        {
            entity.Property(x => x.FollowUpType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.Property(x => x.DueAt).IsRequired();
            entity.HasIndex(x => x.DueAt);
            entity.HasIndex(x => new { x.Status, x.DueAt });
        });
    }
}
