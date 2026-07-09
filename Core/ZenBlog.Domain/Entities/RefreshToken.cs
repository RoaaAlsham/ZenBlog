using ZenBlog.Domain.Entities.Common;

namespace ZenBlog.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public virtual AppUser User { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
