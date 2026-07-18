namespace ZenBlog.Application.Models;

/// <summary>
/// Bound from the "CloudinarySettings" section (User Secrets / env vars).
/// Kept in Application so validators can enforce delivery-URL rules without
/// referencing Infrastructure.
/// </summary>
public class CloudinarySettings
{
    public const string SectionName = "CloudinarySettings";

    public string CloudName { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string ApiSecret { get; set; } = default!;
}
