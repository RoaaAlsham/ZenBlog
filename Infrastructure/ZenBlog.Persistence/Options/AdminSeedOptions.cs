namespace ZenBlog.Persistence.Options;

/// <summary>
/// Bound from the "AdminSeed" configuration section.
/// Email and Password must come from User Secrets / environment variables — never commit them.
/// </summary>
public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; }

    /// <summary>
    /// Bootstrap admin email. Set via User Secrets or environment variables — never commit.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = "admin";

    public string FirstName { get; set; } = "Site";

    public string LastName { get; set; } = "Admin";

    /// <summary>
    /// Bootstrap admin password. Set via User Secrets or environment variables — never commit.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
