namespace ZenBlog.Application.Features.Monitoring.Results;

public sealed record ActivityLogResult(
    Guid Id,
    DateTime OccurredAtUtc,
    string? ActorUserId,
    string? ActorDisplayName,
    string Action,
    string? EntityType,
    string? EntityId,
    string Summary,
    bool Success);
