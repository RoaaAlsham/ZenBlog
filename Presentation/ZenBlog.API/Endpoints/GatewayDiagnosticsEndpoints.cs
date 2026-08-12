using ZenBlog.API.CustomMiddlewares;

namespace ZenBlog.API.Endpoints
{
    /// <summary>
    /// Temporary diagnostic: reports what the AuthDeep gateway actually forwards to this
    /// service.
    ///
    /// The identity-bridge design turns on one question that cannot be answered by reading
    /// code on either side — when the caller authenticates with a service key (`sak_`), does
    /// the gateway inject an end-user identity, or only the service's own? The gateway knows
    /// the calling app, not the reader. This endpoint answers it by observation.
    ///
    /// Disabled unless <c>Diagnostics:GatewayIdentity</c> is true, so it cannot ship enabled
    /// by accident. Delete this file once the question is settled.
    /// </summary>
    public static class GatewayDiagnosticsEndpoints
    {
        private const string EnabledConfigKey = "Diagnostics:GatewayIdentity";

        /// <summary>
        /// Headers that are credentials rather than identity. Their presence and length are
        /// reported; the values are not, so a diagnostic response can never leak the gateway
        /// key or a live signature.
        /// </summary>
        private static readonly string[] CredentialHeaders =
        [
            "X-Gateway-Key",
            "X-Gateway-Signature"
        ];

        public static void RegisterGatewayDiagnosticsEndpoints(this IEndpointRouteBuilder erb)
        {
            var configuration = erb.ServiceProvider.GetRequiredService<IConfiguration>();
            if (!configuration.GetValue<bool>(EnabledConfigKey))
            {
                return;
            }

            // Lives under /api and is not an anonymous read prefix, so
            // AuthDeepProtectedRoutes makes the gateway signature mandatory — it is
            // unreachable except through the gateway.
            //
            // Deliberately NOT RequireAuthorization: the entire question is what arrives
            // when nothing has yet authenticated the end user. Adding the guard would make
            // it answer 401 and tell us nothing.
            erb.MapGet("/diagnostics/gateway-identity", (HttpContext context) =>
            {
                context.Items.TryGetValue(AuthDeepGatewayMiddleware.IdentityItemKey, out var stashed);
                var identity = stashed as AuthDeepIdentity;

                var forwarded = context.Request.Headers
                    .Where(header =>
                        header.Key.StartsWith("X-AuthDeep-", StringComparison.OrdinalIgnoreCase)
                        || header.Key.StartsWith("X-Gateway-", StringComparison.OrdinalIgnoreCase)
                        || header.Key.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        header => header.Key,
                        header => CredentialHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                            ? $"<present, {header.Value.ToString().Length} chars>"
                            : header.Value.ToString(),
                        StringComparer.OrdinalIgnoreCase);

                return Results.Ok(new
                {
                    // True only if the middleware ran and the HMAC verified.
                    SignatureVerified = identity is not null,
                    ParsedIdentity = identity is null
                        ? null
                        : new
                        {
                            identity.UserId,
                            identity.TenantId,
                            identity.Email,
                            identity.Roles,
                            identity.RequestId
                        },
                    // Every AuthDeep/gateway header that actually arrived, in full, so a
                    // header the middleware does not yet read is still visible here.
                    ForwardedHeaders = forwarded,
                    // Expected false until the gateway identity auth scheme exists.
                    PrincipalIsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
                    PrincipalName = context.User.Identity?.Name
                });
            });
        }
    }
}
