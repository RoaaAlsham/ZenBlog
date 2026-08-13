using ZenBlog.API.CustomMiddlewares;

namespace ZenBlog.API.Endpoints
{
    /// <summary>
    /// Temporary diagnostic: reports what the AuthDeep gateway actually forwards to this
    /// service.
    ///
    /// The identity-bridge design turns on one question that cannot be answered by reading
    /// code on either side — when the caller authenticates with a service key (`sak_`), what
    /// reaches the backend? Measurement has already shown the published header list does not
    /// match the wire for that case, so this reports headers <em>exhaustively</em> rather
    /// than through an allowlist: a filtered view cannot distinguish "the gateway stripped
    /// it" from "we forgot to look for it".
    ///
    /// Disabled unless <c>Diagnostics:GatewayIdentity</c> is true, so it cannot ship enabled
    /// by accident. Delete this file once the question is settled.
    /// </summary>
    public static class GatewayDiagnosticsEndpoints
    {
        private const string EnabledConfigKey = "Diagnostics:GatewayIdentity";

        /// <summary>
        /// Headers whose values are credentials. Everything else is echoed verbatim; these
        /// are reported as presence and length only, so a diagnostic response stays safe to
        /// paste into a ticket. Redaction is deliberately visible rather than silent — a
        /// missing header and a hidden one must not look alike.
        /// </summary>
        private static readonly HashSet<string> CredentialHeaders =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Authorization",
                "Proxy-Authorization",
                "Cookie",
                "X-API-Key",
                "X-HMAC-Signature",
                "X-Gateway-Key",
                "X-Gateway-Signature"
            };

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
                var request = context.Request;

                // Sorted so two runs diff cleanly against each other.
                var headers = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // A header sent more than once arrives comma-joined, which would silently
                // corrupt an id. Naming them separately makes that visible instead.
                var duplicated = new List<string>();

                foreach (var header in request.Headers)
                {
                    headers[header.Key] = CredentialHeaders.Contains(header.Key)
                        ? $"<present, {header.Value.ToString().Length} chars>"
                        : header.Value.ToString();

                    if (header.Value.Count > 1)
                    {
                        duplicated.Add($"{header.Key} (x{header.Value.Count})");
                    }
                }

                return Results.Ok(new
                {
                    // True only if the middleware ran and the HMAC verified.
                    SignatureVerified = identity is not null,
                    ParsedIdentity = identity is null
                        ? null
                        : new
                        {
                            AuthType = identity.AuthType.ToString(),
                            // The whole point of the sak_ test: these four must all be
                            // empty for an api_key caller, however the call was dressed up.
                            identity.UserId,
                            identity.Email,
                            identity.Roles,
                            identity.IsHuman,
                            identity.TenantId,
                            identity.ApiKeyId,
                            identity.ApiKeyType,
                            identity.RequestId
                        },
                    PrincipalIsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
                    PrincipalName = context.User.Identity?.Name,
                    // Proves the gateway identity reached the authorization layer, not
                    // just the middleware — this is what RequireRole("Admin") reads.
                    PrincipalRoles = context.User.Claims
                        .Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(claim => claim.Value)
                        .ToArray(),
                    // Shows whether the gateway rewrites the request line, not just headers.
                    Request = new
                    {
                        request.Method,
                        Path = request.Path.Value,
                        QueryString = request.QueryString.Value,
                        request.Protocol,
                        request.ContentLength,
                        Scheme = request.Scheme,
                        Host = request.Host.Value
                    },
                    HeaderCount = headers.Count,
                    DuplicatedHeaders = duplicated,
                    Headers = headers
                });
            });
        }
    }
}
