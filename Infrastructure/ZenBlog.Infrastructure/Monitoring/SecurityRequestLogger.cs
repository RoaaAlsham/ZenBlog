using Microsoft.Extensions.Logging;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Monitoring;

public sealed class SecurityRequestLogger(
    IRepository<SecurityRequestLog> repository,
    IUnitOfWork unitOfWork,
    IClientRequestInfo clientRequestInfo,
    ILogger<SecurityRequestLogger> logger) : ISecurityRequestLogger
{
    public async Task LogAsync(
        SecurityEventType eventType,
        int statusCode,
        string? sourceIp = null,
        string? host = null,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            await repository.CreateAsync(new SecurityRequestLog
            {
                OccurredAtUtc = now,
                SourceIp = Truncate(sourceIp ?? clientRequestInfo.SourceIp, 64),
                Host = Truncate(host ?? clientRequestInfo.Host, 256),
                Path = Truncate(path ?? clientRequestInfo.Path, 2048),
                EventType = eventType,
                StatusCode = statusCode
            });
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist security request log for {EventType}", eventType);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
