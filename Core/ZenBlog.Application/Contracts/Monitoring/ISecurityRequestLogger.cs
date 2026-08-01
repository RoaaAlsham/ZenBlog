using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Contracts.Monitoring;

public interface ISecurityRequestLogger
{
    Task LogAsync(
        SecurityEventType eventType,
        int statusCode,
        string? sourceIp = null,
        string? host = null,
        string? path = null,
        CancellationToken cancellationToken = default);
}
