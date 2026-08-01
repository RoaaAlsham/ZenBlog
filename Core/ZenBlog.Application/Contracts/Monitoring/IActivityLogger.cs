namespace ZenBlog.Application.Contracts.Monitoring;

public interface IActivityLogger
{
    Task LogAsync(
        string action,
        string summary,
        string? actorUserId = null,
        string? actorDisplayName = null,
        string? entityType = null,
        string? entityId = null,
        bool success = true,
        CancellationToken cancellationToken = default);
}
