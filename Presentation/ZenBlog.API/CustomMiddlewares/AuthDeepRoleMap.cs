namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Reconciles AuthDeep's role names with this service's.
    ///
    /// AuthDeep asserts roles in its own vocabulary and its own casing — `admin`,
    /// `tenant_admin`, `global_admin` — while every authorization check here is written
    /// as <c>RequireRole("Admin")</c>. Role claims are compared ordinally, so without a
    /// mapping a tenant admin would authenticate perfectly and then be refused by every
    /// admin endpoint, which is a confusing failure to debug.
    ///
    /// The mapping only ever *adds* the canonical name; the role AuthDeep sent is kept
    /// alongside it, so nothing downstream loses information.
    /// </summary>
    public static class AuthDeepRoleMap
    {
        /// <summary>The role name this service's policies are written against.</summary>
        public const string AdminRole = "Admin";

        /// <summary>
        /// Extra role names that should grant admin, comma-separated. Mirrors the client's
        /// NEXT_PUBLIC_ADMIN_ROLES so both sides can be widened together.
        /// </summary>
        public const string AdminRolesConfigKey = "AuthDeep:AdminRoles";

        private static readonly string[] DefaultAdminAliases =
        [
            "admin",
            "tenant_admin",
            "global_admin",
            "super_admin"
        ];

        public static IReadOnlySet<string> AdminAliases(IConfiguration configuration)
        {
            var configured = configuration[AdminRolesConfigKey]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            return new HashSet<string>(
                DefaultAdminAliases.Concat(configured),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when <paramref name="role"/> should also grant <see cref="AdminRole"/>.
        /// Never true for the canonical name itself, which would only duplicate a claim.
        /// </summary>
        public static bool GrantsAdmin(IReadOnlySet<string> aliases, string role) =>
            !string.Equals(role, AdminRole, StringComparison.Ordinal)
            && aliases.Contains(role);
    }
}
