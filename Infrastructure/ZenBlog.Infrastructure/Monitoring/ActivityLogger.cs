using Microsoft.Extensions.Logging;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Monitoring;

public sealed class ActivityLogger(
    IRepository<ActivityLog> repository,
    IUnitOfWork unitOfWork,
    ILogger<ActivityLogger> logger) : IActivityLogger
{
    public async Task LogAsync(
        string action,
        string summary,
        string? actorUserId = null,
        string? actorDisplayName = null,
        string? entityType = null,
        string? entityId = null,
        bool success = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            await repository.CreateAsync(new ActivityLog
            {
                OccurredAtUtc = now,
                ActorUserId = actorUserId,
                ActorDisplayName = actorDisplayName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary.Length > 500 ? summary[..500] : summary,
                Success = success
            });
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist activity log for action {Action}", action);
        }
    }
}
