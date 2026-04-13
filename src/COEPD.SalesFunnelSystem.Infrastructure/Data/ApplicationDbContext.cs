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
    public DbSet<LeadStageTransition> LeadStageTransitions => Set<LeadStageTransition>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
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
            entity.Property(x => x.StageEnteredAtUtc).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Phone).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.UpdatedAt);
            entity.HasIndex(x => x.StageEnteredAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Source);
            entity.HasIndex(x => x.Domain);
            entity.HasIndex(x => x.AssignedStaffId);
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
            entity.HasOne(x => x.AssignedStaff)
                .WithMany(x => x.AssignedLeads)
                .HasForeignKey(x => x.AssignedStaffId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DemoBooking>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.Day).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Slot).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.HasOne(x => x.Lead).WithMany(x => x.DemoBookings).HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => new { x.Day, x.Slot, x.Status });
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
            entity.ToTable("Users");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FailedLoginAttempts).HasDefaultValue(0).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => new { x.Role, x.IsActive });
        });

        modelBuilder.Entity<LeadActivityLog>(entity =>
        {
            entity.ToTable("LeadActivities");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.ActivityType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.Lead).WithMany(x => x.Activities).HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.LeadActivities).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.LeadId, x.CreatedAt });
        });

        modelBuilder.Entity<FunnelEvent>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.Stage).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Timestamp).IsRequired();
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.Stage);
        });

        modelBuilder.Entity<LeadFollowUpJob>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.FollowUpType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.Property(x => x.DueAt).IsRequired();
            entity.HasIndex(x => x.DueAt);
            entity.HasIndex(x => new { x.Status, x.DueAt });
        });

        modelBuilder.Entity<LeadStageTransition>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.FromStage).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ToStage).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ChangedAtUtc).IsRequired();
            entity.HasOne(x => x.Lead)
                .WithMany(x => x.StageTransitions)
                .HasForeignKey(x => x.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.LeadStageTransitions)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.LeadId);
            entity.HasIndex(x => x.ChangedByUserId);
            entity.HasIndex(x => new { x.ToStage, x.ChangedAtUtc });
        });
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<COEPD.SalesFunnelSystem.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
