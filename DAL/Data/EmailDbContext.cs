using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace DAL.Data;

/// <summary>
/// Database context for Email Assistant application
/// </summary>
public class EmailDbContext : DbContext
{
    public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
    {
    }

    public DbSet<Email> Emails { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Email entity
        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasIndex(e => e.SenderEmail);
            entity.HasIndex(e => e.ReceivedTime);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.ExternalEmailId).IsUnique().HasFilter("[ExternalEmailId] IS NOT NULL");

            entity.Property(e => e.Subject).IsRequired();
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.SenderName).IsRequired();
            entity.Property(e => e.SenderEmail).IsRequired();
            entity.Property(e => e.ReceivedTime).IsRequired();

            // Relationship with Category
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Emails)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Priority);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired();
        });

        // Seed initial categories
        SeedCategories(modelBuilder);
    }

    /// <summary>
    /// Seed default categories
    /// </summary>
    private void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Most Important",
                DisplayName = "🔥 Most Important",
                Description = "Urgent and critical emails requiring immediate attention",
                ColorCode = "#dc3545", // Red
                Priority = 1,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 2,
                Name = "Important",
                DisplayName = "⭐ Important",
                Description = "Important emails that need attention but are not urgent",
                ColorCode = "#ffc107", // Yellow
                Priority = 2,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 3,
                Name = "Casual",
                DisplayName = "🙂 Casual",
                Description = "Regular casual emails",
                ColorCode = "#0d6efd", // Blue (default)
                Priority = 3,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 4,
                Name = "Promotional",
                DisplayName = "🏷️ Promotional",
                Description = "Promotional and marketing emails",
                ColorCode = "#6c757d", // Gray
                Priority = 4,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}


