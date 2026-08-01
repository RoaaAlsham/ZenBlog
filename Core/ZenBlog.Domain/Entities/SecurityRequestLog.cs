using ZenBlog.Domain.Entities.Common;

namespace ZenBlog.Domain.Entities;

public class SecurityRequestLog : BaseEntity
{
    public DateTime OccurredAtUtc { get; set; }
    public required string SourceIp { get; set; }
    public required string Host { get; set; }
    public required string Path { get; set; }
    public SecurityEventType EventType { get; set; }
    public int StatusCode { get; set; }
}
