using Microsoft.AspNetCore.Identity;

namespace ZenBlog.Domain.Entities;
public class AppUser : IdentityUser<string>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? ImageUrl { get; set; }

    public virtual ICollection<Blog> Blogs { get; set; } = [];
    public virtual ICollection<Comment> Comments { get; set; } = [];
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
// Lazy Loading is not enabled by default in EF Core,
// so we need to explicitly mark the navigation properties
// as virtual to enable it. This allows EF Core to create
// proxy classes that can load related entities on demand when they are accessed.
//Instead of fetching all related data at once, you load it on demand 