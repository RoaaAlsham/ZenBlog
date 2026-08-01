using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Monitoring.Results;

public sealed record SecurityRequestLogResult(
    Guid Id,
    DateTime OccurredAtUtc,
    string SourceIp,
    string Host,
    string Path,
    SecurityEventType EventType,
    int StatusCode);
