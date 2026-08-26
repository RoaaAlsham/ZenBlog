namespace ZenBlog.Application.Contracts.Identity
{
    /// <summary>
    /// Role names this service's authorization is written against.
    ///
    /// AuthDeep asserts roles in its own vocabulary (admin, tenant_admin, global_admin);
    /// the API layer maps those onto the canonical names here before any handler sees
    /// them, so a handler never has to know AuthDeep's spelling.
    /// </summary>
    public static class ApplicationRoles
    {
        public const string Admin = "Admin";
    }
}
