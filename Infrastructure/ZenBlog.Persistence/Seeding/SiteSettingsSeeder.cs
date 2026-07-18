using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZenBlog.Persistence.Context;

namespace ZenBlog.Persistence.Seeding;

public static class SiteSettingsSeeder
{
    /// <summary>
    /// Ensures the singleton SiteSettings row exists (AllowRegistrations=false by default).
    /// Idempotent. Safe fallback when migrations/HasData were not applied (e.g. InMemory tests).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SiteSettingsSeeder).FullName!);
        var db = serviceProvider.GetRequiredService<AppDbContext>();

        var singletonId = ZenBlog.Domain.Entities.SiteSettings.SingletonId;
        var existing = await db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == singletonId);

        if (existing is not null)
        {
            logger.LogDebug("SiteSettings singleton already exists.");
            return;
        }

        var now = DateTime.UtcNow;
        db.SiteSettings.Add(new ZenBlog.Domain.Entities.SiteSettings
        {
            Id = singletonId,
            AllowRegistrations = false,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Created SiteSettings singleton with AllowRegistrations=false.");
    }
}
