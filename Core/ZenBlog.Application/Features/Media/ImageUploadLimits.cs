namespace ZenBlog.Application.Features.Media;

public static class ImageUploadLimits
{
    /// <summary>Application validation limit (5 MB).</summary>
    public const long MaxBytes = 5L * 1024 * 1024;

    /// <summary>
    /// Kestrel / multipart hard limit — slightly above <see cref="MaxBytes"/>
    /// so valid 5 MB payloads are not cut off by framing overhead.
    /// </summary>
    public const long MultipartHardLimitBytes = 6L * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public static string FolderFor(ImageUploadPurpose purpose) => purpose switch
    {
        ImageUploadPurpose.Profile => "zenblog/profiles",
        ImageUploadPurpose.BlogCover => "zenblog/covers",
        ImageUploadPurpose.BlogBody => "zenblog/content",
        _ => "zenblog/misc"
    };
}
