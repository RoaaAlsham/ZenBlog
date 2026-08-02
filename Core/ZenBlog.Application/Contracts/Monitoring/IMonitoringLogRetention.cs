namespace ZenBlog.Application.Contracts.Monitoring;

public interface IMonitoringLogRetention
{
    Task PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
