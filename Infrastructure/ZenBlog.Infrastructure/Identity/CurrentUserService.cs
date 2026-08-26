using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ZenBlog.Application.Contracts.Identity;

namespace ZenBlog.Infrastructure.Identity
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public string? UserId =>
            httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public bool IsAuthenticated =>
            httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public IReadOnlyList<string> Roles =>
            httpContextAccessor.HttpContext?.User?
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray()
            ?? [];

        /// <summary>
        /// Read from the same gateway-asserted claims as <see cref="Roles"/>, never from
        /// the local role table. The comparison is case-insensitive because AuthDeep's
        /// casing is preserved when the alias is mapped, and an ordinal test would deny a
        /// legitimate admin over a capital letter.
        /// </summary>
        public bool IsAdmin =>
            Roles.Any(role => string.Equals(role, ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase));
    }
}
