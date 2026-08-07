namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Decides which requests must carry an AuthDeep gateway signature.
    /// Deny-by-default: everything under /api is protected unless it is an anonymous
    /// GET read that the public site fetches directly, so a newly added endpoint is
    /// protected automatically rather than accidentally left open.
    /// </summary>
    public static class AuthDeepProtectedRoutes
    {
        /// <summary>
        /// Route prefixes whose GET handlers are all mapped without RequireAuthorization.
        /// Matched per path segment, so "/api/blogsecret" does NOT match "/api/blogs".
        /// Non-GET verbs on these same prefixes stay protected.
        /// </summary>
        private static readonly string[] AnonymousReadPrefixes =
        [
            "/api/blogs",              // list, by id, by category, by user
            "/api/categories",         // list, by id
            "/api/comments",           // by blog, by id
            "/api/settings",           // public site settings
            "/api/users/by-username"   // public author profile; /api/users/me and /api/users stay protected
        ];

        public static bool RequiresGatewaySignature(HttpContext context)
        {
            var request = context.Request;

            // Not our API surface (/health, /openapi, /scalar) -> never protected.
            if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // CORS preflight never carries gateway headers. UseCors runs first and normally
            // short-circuits these, but the carve-out keeps the branch correct if order changes.
            if (HttpMethods.IsOptions(request.Method))
            {
                return false;
            }

            // Every write verb under /api is protected, no exceptions.
            if (!HttpMethods.IsGet(request.Method))
            {
                return true;
            }

            foreach (var prefix in AnonymousReadPrefixes)
            {
                if (request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
