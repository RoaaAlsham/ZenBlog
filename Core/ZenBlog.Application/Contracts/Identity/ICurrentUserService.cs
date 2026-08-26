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

        /// <summary>
        /// True when AuthDeep asserted an admin role for this request.
        ///
        /// The gateway is the only source: Roles comes from the signature-verified
        /// X-AuthDeep-User-Roles header, and the API layer has already mapped AuthDeep's
        /// vocabulary (admin, tenant_admin, global_admin) onto the canonical "Admin" this
        /// property tests for. Asking the local AspNetUserRoles table instead would always
        /// answer "no" for an AuthDeep reader, because roles are deliberately never
        /// written there.
        /// </summary>
        bool IsAdmin { get; }
    }
}
