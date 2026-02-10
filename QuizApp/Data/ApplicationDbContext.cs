using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TestAttempt> Attempts => Set<TestAttempt>();

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
            .HasMaxLength(200);
    }
}

