namespace ZenBlog.Application.Contracts.Identity
{
    // Lets MediatR handlers ask "who is calling me?" without depending on
    // Microsoft.AspNetCore.Http (Application must stay framework-agnostic).
    // Implemented in Infrastructure using IHttpContextAccessor.
    public interface ICurrentUserService
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }

        /// <summary>
        /// Roles AuthDeep asserted for this request, read from the gateway-injected
        /// header after its signature verified. This service does not add to them: the
        /// local AspNetUserRoles table no longer drives authorization.
        /// </summary>
        IReadOnlyList<string> Roles { get; }
    }
}
