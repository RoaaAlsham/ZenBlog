using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZenBlog.Domain.Entities;
namespace ZenBlog.Persistence.Context
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        /*
         Each DbSet<T> does three things simultaneously: it represents a table in your database,
         it's the entry point for all LINQ queries against that table,
         and it's the collection you add/remove entities from.
         */
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Category> Categories { get; set; }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<SecurityRequestLog> SecurityRequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);// required to run Identity's config first

            modelBuilder.Entity<SiteSettings>(entity =>
            {
                entity.HasData(new SiteSettings
                {
                    Id = ZenBlog.Domain.Entities.SiteSettings.SingletonId,
                    AllowRegistrations = false,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            });

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasIndex(a => a.OccurredAtUtc);
                entity.HasIndex(a => new { a.Action, a.OccurredAtUtc });
                entity.Property(a => a.Action).HasMaxLength(100);
                entity.Property(a => a.ActorUserId).HasMaxLength(450);
                entity.Property(a => a.ActorDisplayName).HasMaxLength(256);
                entity.Property(a => a.EntityType).HasMaxLength(100);
                entity.Property(a => a.EntityId).HasMaxLength(450);
                entity.Property(a => a.Summary).HasMaxLength(500);
            });

            modelBuilder.Entity<SecurityRequestLog>(entity =>
            {
                entity.HasIndex(s => s.OccurredAtUtc);
                entity.HasIndex(s => new { s.EventType, s.OccurredAtUtc });
                entity.Property(s => s.SourceIp).HasMaxLength(64);
                entity.Property(s => s.Host).HasMaxLength(256);
                entity.Property(s => s.Path).HasMaxLength(2048);
            });

            modelBuilder.Entity<Blog>(entity =>
            {
                // Prevent deleting a category from wiping all of its blogs.
                entity.HasOne(b => b.Category)
                      .WithMany(c => c.Blogs)
                      .HasForeignKey(b => b.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.ParentComment)
                      .WithMany(c => c.Replies)
                      .HasForeignKey(c => c.ParentCommentId)
                      .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete cycles

                entity.HasOne(c => c.Blog)
                      .WithMany(b => b.Comments)
                      .HasForeignKey(c => c.BlogId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(rt => rt.TokenHash).IsUnique();
                entity.HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc });

                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}