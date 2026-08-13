namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// How the caller authenticated to the gateway. This is the field that decides
    /// whether there is a human behind the request at all.
    /// </summary>
    /// <remarks>
    /// The gateway does not send an auth-type header; it distinguishes callers by which
    /// identity headers it injects. <see cref="Human"/> is therefore the value seen in
    /// practice for a person, because the injected headers do not say whether they arrived
    /// by session cookie or by web token. <see cref="Session"/> and <see cref="WebToken"/>
    /// exist for the day AuthDeep states it explicitly.
    /// </remarks>
    public enum AuthDeepAuthType
    {
        /// <summary>Neither a user id nor an API key id was injected — an anonymous hop.</summary>
        Unknown = 0,

        /// <summary>A person, by a means the gateway did not name. The usual human value.</summary>
        Human,

        /// <summary>Browser session cookie on an AuthDeep-hosted origin (path A), stated explicitly.</summary>
        Session,

        /// <summary>Web token (`wat_`) plus a proof-of-possession signature (path B), stated explicitly.</summary>
        WebToken,

        /// <summary>Service or client API key (`sak_`/`cak_`) — a machine, never a person (path C).</summary>
        ApiKey
    }

    /// <summary>
    /// Caller identity asserted by AuthDeep. Only ever constructed after the request
    /// signature verifies, so the headers it carries are safe to trust downstream.
    /// Read it with <c>HttpContext.Items[AuthDeepGatewayMiddleware.IdentityItemKey]</c>.
    /// </summary>
    /// <param name="AuthType">Inferred from the injected headers; see <see cref="AuthDeepAuthType"/>.</param>
    /// <param name="UserId">X-AuthDeep-User-ID (or X-Forwarded-User-Id) — human paths only</param>
    /// <param name="TenantId">X-AuthDeep-Tenant-ID — authoritative, on every forwarded request</param>
    /// <param name="Email">X-AuthDeep-User-Email — human paths only</param>
    /// <param name="Roles">X-AuthDeep-User-Roles, split on commas — human paths only</param>
    /// <param name="ApiKeyId">X-AuthDeep-API-Key-ID — API key path only</param>
    /// <param name="ApiKeyType">X-AuthDeep-API-Key-Type — API key path only</param>
    /// <param name="RequestId">X-Gateway-Request-Id, for correlating with gateway logs</param>
    public sealed record AuthDeepIdentity(
        AuthDeepAuthType AuthType,
        string? UserId,
        string? TenantId,
        string? Email,
        IReadOnlyList<string> Roles,
        string? ApiKeyId,
        string? ApiKeyType,
        string? RequestId)
    {
        /// <summary>
        /// True when a real person is behind this request.
        /// </summary>
        /// <remarks>
        /// An API key is explicitly not a human, however many user-shaped headers a
        /// caller may have tried to attach: the gateway injects no user identity for
        /// one, so anything user-shaped arriving alongside <see cref="AuthDeepAuthType.ApiKey"/>
        /// is spoofed and is discarded before this record is built — which is why
        /// <see cref="UserId"/> is already null in that case.
        ///
        /// What makes someone a human here is an injected user id, not a label — which is
        /// exactly right given the gateway never sends a label.
        /// </remarks>
        public bool IsHuman =>
            AuthType is not AuthDeepAuthType.ApiKey
            && !string.IsNullOrWhiteSpace(UserId);
    }
}
