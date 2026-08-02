using Microsoft.Extensions.Logging;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Monitoring;

public sealed class MonitoringLogRetention(
    IRepository<ActivityLog> activityRepository,
    IRepository<SecurityRequestLog> securityRepository,
    ILogger<MonitoringLogRetention> logger) : IMonitoringLogRetention
{
    public async Task PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - MonitoringRetention.MaxAge;

        var activityDeleted = await activityRepository.DeleteWhereAsync(
            a => a.OccurredAtUtc < cutoff,
            cancellationToken);

        var securityDeleted = await securityRepository.DeleteWhereAsync(
            s => s.OccurredAtUtc < cutoff,
            cancellationToken);

        if (activityDeleted > 0 || securityDeleted > 0)
        {
            logger.LogInformation(
                "Purged monitoring logs older than {Cutoff:o}: {ActivityCount} activity, {SecurityCount} security",
                cutoff,
                activityDeleted,
                securityDeleted);
        }
    }
}
