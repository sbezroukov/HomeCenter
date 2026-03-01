using Microsoft.EntityFrameworkCore;
using HomeCenter.Models;

namespace HomeCenter.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TestAttempt> Attempts => Set<TestAttempt>();
    public DbSet<TestHistoryEntry> TestHistory => Set<TestHistoryEntry>();
    
    // Calendar tables
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<ScheduledActivity> ScheduledActivities => Set<ScheduledActivity>();
    public DbSet<ActivityCompletion> ActivityCompletions => Set<ActivityCompletion>();
    public DbSet<ActivityPhoto> ActivityPhotos => Set<ActivityPhoto>();
    public DbSet<ActivityComment> ActivityComments => Set<ActivityComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.UserName)
            .HasMaxLength(100);

        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.Password)
            .HasMaxLength(200);

        modelBuilder.Entity<Topic>()
            .HasIndex(t => t.FileName)
            .IsUnique();

        modelBuilder.Entity<Topic>()
            .Property(t => t.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<Topic>()
            .Property(t => t.FileName)
            .HasMaxLength(500);

        modelBuilder.Entity<TestHistoryEntry>()
            .Property(h => h.FileName)
            .HasMaxLength(500);

        modelBuilder.Entity<TestHistoryEntry>()
            .Property(h => h.FolderPath)
            .HasMaxLength(500);

        // Calendar entities configuration
        modelBuilder.Entity<ActivityType>()
            .HasIndex(at => at.Name);

        modelBuilder.Entity<ScheduledActivity>()
            .HasOne(sa => sa.ActivityType)
            .WithMany(at => at.Activities)
            .HasForeignKey(sa => sa.ActivityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduledActivity>()
            .HasOne(sa => sa.AssignedToUser)
            .WithMany()
            .HasForeignKey(sa => sa.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ScheduledActivity>()
            .HasOne(sa => sa.CreatedByUser)
            .WithMany()
            .HasForeignKey(sa => sa.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduledActivity>()
            .HasIndex(sa => sa.StartDate);

        modelBuilder.Entity<ScheduledActivity>()
            .HasIndex(sa => sa.DeadlineDateTime);

        modelBuilder.Entity<ActivityCompletion>()
            .HasOne(ac => ac.ScheduledActivity)
            .WithMany(sa => sa.Completions)
            .HasForeignKey(ac => ac.ScheduledActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityCompletion>()
            .HasOne(ac => ac.CompletedByUser)
            .WithMany()
            .HasForeignKey(ac => ac.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityCompletion>()
            .HasIndex(ac => ac.CompletedAt);

        modelBuilder.Entity<ActivityCompletion>()
            .HasOne(ac => ac.ApprovedByUser)
            .WithMany()
            .HasForeignKey(ac => ac.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityCompletion>()
            .HasOne(ac => ac.TestAttempt)
            .WithMany()
            .HasForeignKey(ac => ac.TestAttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Activity Photos
        modelBuilder.Entity<ActivityPhoto>()
            .HasOne(ap => ap.ScheduledActivity)
            .WithMany(sa => sa.Photos)
            .HasForeignKey(ap => ap.ScheduledActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityPhoto>()
            .HasOne(ap => ap.UploadedByUser)
            .WithMany()
            .HasForeignKey(ap => ap.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityPhoto>()
            .HasIndex(ap => ap.UploadedAt);

        // Activity Comments
        modelBuilder.Entity<ActivityComment>()
            .HasOne(ac => ac.ScheduledActivity)
            .WithMany(sa => sa.Comments)
            .HasForeignKey(ac => ac.ScheduledActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityComment>()
            .HasOne(ac => ac.AuthorUser)
            .WithMany()
            .HasForeignKey(ac => ac.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityComment>()
            .HasOne(ac => ac.ParentComment)
            .WithMany(pc => pc.Replies)
            .HasForeignKey(ac => ac.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityComment>()
            .HasIndex(ac => ac.CreatedAt);

        // Test integration
        modelBuilder.Entity<ScheduledActivity>()
            .HasOne(sa => sa.TestTopic)
            .WithMany()
            .HasForeignKey(sa => sa.TestTopicId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

