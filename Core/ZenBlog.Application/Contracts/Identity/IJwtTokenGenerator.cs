using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Contracts.Identity
{
    // This is a "port" in Clean Architecture terms: Application declares what it needs
    // ("give me a signed token for this user") without knowing HOW it is built.
    // ZenBlog.Infrastructure provides the concrete implementation (see JwtTokenGenerator).
    public interface IJwtTokenGenerator
    {
        // Returns both the token and the exact expiry it was signed with, so callers
        // never have to duplicate/guess the ExpiryMinutes value from JwtSettings.
        (string Token, DateTime ExpiresAtUtc) GenerateToken(AppUser user, IList<string> roles);
    }
}
