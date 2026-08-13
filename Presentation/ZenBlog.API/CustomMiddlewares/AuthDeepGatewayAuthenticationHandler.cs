using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ZenBlog.API.CustomMiddlewares
{
    public static class AuthDeepGatewayDefaults
    {
        public const string AuthenticationScheme = "AuthDeepGateway";
    }

    /// <summary>
    /// Turns the gateway-injected identity into the <see cref="ClaimsPrincipal"/> that
    /// <c>.RequireAuthorization()</c> and <c>RequireRole("Admin")</c> read.
    ///
    /// This handler validates nothing. <see cref="AuthDeepGatewayMiddleware"/> has already
    /// recomputed the HMAC over the whole request and rejected anything that did not come
    /// from the gateway, and it stashed the identity only after that succeeded. By the time
    /// this runs, the question "is this real?" is settled; the only job left is translation.
    ///
    /// Because the middleware runs in a <c>UseWhen</c> branch, an unprotected route reaches
    /// here with nothing stashed and authenticates as nobody — which is the correct answer
    /// for an anonymous public read.
    /// </summary>
    public sealed class AuthDeepGatewayAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        /// <summary>Claim carrying session | web_token | api_key, for logging and policy.</summary>
        public const string AuthTypeClaim = "authdeep:auth_type";

        /// <summary>Claim carrying the tenant the gateway resolved.</summary>
        public const string TenantClaim = "authdeep:tenant_id";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Items.TryGetValue(AuthDeepGatewayMiddleware.IdentityItemKey, out var stashed)
                || stashed is not AuthDeepIdentity identity)
            {
                // Either an unprotected route, or the middleware rejected the request
                // before reaching here. Nothing to authenticate.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!identity.IsHuman)
            {
                // A verified machine caller. It is authenticated *to the gateway*, but it
                // is not a user, and minting a principal for it would let an API key
                // satisfy an endpoint written for a person.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, identity.UserId!),
                new(AuthTypeClaim, identity.AuthType.ToString())
            };

            if (identity.Email is not null)
            {
                claims.Add(new Claim(ClaimTypes.Email, identity.Email));
                // Name is what HttpContext.User.Identity.Name surfaces; email is the only
                // human-readable handle the gateway sends.
                claims.Add(new Claim(ClaimTypes.Name, identity.Email));
            }

            if (identity.TenantId is not null)
            {
                claims.Add(new Claim(TenantClaim, identity.TenantId));
            }

            // ClaimTypes.Role specifically: RequireRole and IsInRole both look here.
            // AuthDeep's own casing is preserved, and a recognised admin alias also gets
            // the canonical "Admin" so RequireRole("Admin") matches — see AuthDeepRoleMap.
            var adminAliases = AuthDeepRoleMap.AdminAliases(configuration);
            var seenAdmin = false;

            foreach (var role in identity.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

                if (!seenAdmin && AuthDeepRoleMap.GrantsAdmin(adminAliases, role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, AuthDeepRoleMap.AdminRole));
                    seenAdmin = true;
                }
            }

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, AuthDeepGatewayDefaults.AuthenticationScheme));

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, AuthDeepGatewayDefaults.AuthenticationScheme)));
        }

        /// <summary>
        /// Answers 401 without a WWW-Authenticate challenge.
        ///
        /// There is nothing for the caller to retry against this service: credentials are
        /// minted at AuthDeep and arrive already verified, so advertising a scheme here
        /// would only invite direct attempts that the gateway middleware rejects anyway.
        /// </summary>
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
    }
}
