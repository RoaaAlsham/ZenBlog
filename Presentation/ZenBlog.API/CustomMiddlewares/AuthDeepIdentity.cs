namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Caller identity asserted by AuthDeep. Only ever constructed after the request
    /// signature verifies, so the headers it carries are safe to trust downstream.
    /// Read it with <c>HttpContext.Items[AuthDeepGatewayMiddleware.IdentityItemKey]</c>.
    /// </summary>
    /// <param name="UserId">X-AuthDeep-User-ID</param>
    /// <param name="TenantId">X-AuthDeep-Tenant-ID</param>
    /// <param name="Email">X-AuthDeep-User-Email</param>
    /// <param name="Roles">X-AuthDeep-User-Roles, split on commas</param>
    /// <param name="RequestId">X-Request-Id, for correlating with gateway logs</param>
    public sealed record AuthDeepIdentity(
        string? UserId,
        string? TenantId,
        string? Email,
        IReadOnlyList<string> Roles,
        string? RequestId);
}
