using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Settings;

internal static class SiteSettingsAccess
{
    /// <summary>
    /// Loads the singleton row, creating it with AllowRegistrations=false if missing.
    /// </summary>
    public static async Task<SiteSettings> GetOrCreateAsync(
        IRepository<SiteSettings> repository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetByIdAsync(SiteSettings.SingletonId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        var now = DateTime.UtcNow;
        settings = new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            AllowRegistrations = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.CreateAsync(settings);
        await unitOfWork.SaveChangesAsync();
        return settings;
    }
}
