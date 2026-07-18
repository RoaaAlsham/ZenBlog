namespace ZenBlog.Application.Features.Media;

public static class CloudinaryImageRules
{
    public static bool BothSetOrBothMissing(string? url, string? publicId)
    {
        var hasUrl = !string.IsNullOrWhiteSpace(url);
        var hasPublicId = !string.IsNullOrWhiteSpace(publicId);
        return hasUrl == hasPublicId;
    }

    public static bool IsCloudinaryDeliveryUrl(string? url, string cloudName)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(cloudName))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        // https://res.cloudinary.com/{cloudName}/image/upload/...
        if (!uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 1
            && segments[0].Equals(cloudName, StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
