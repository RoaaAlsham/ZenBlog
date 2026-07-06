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
    }
}
