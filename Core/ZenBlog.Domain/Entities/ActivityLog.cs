using ZenBlog.Domain.Entities.Common;

namespace ZenBlog.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public DateTime OccurredAtUtc { get; set; }
    public string? ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public required string Summary { get; set; }
    public bool Success { get; set; } = true;
}
