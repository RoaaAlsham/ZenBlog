namespace ZenBlog.Application.Features.Media;

public static class CloudinaryImageRules
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

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

    public static bool TryExtractPublicIdFromDeliveryUrl(
        string? url,
        string cloudName,
        out string? publicId)
    {
        publicId = null;

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

        if (!uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4
            || !segments[0].Equals(cloudName, StringComparison.OrdinalIgnoreCase)
            || !segments[1].Equals("image", StringComparison.OrdinalIgnoreCase)
            || !segments[2].Equals("upload", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var index = 3;
        while (index < segments.Length && segments[index].Contains(','))
        {
            index++;
        }

        if (index < segments.Length
            && segments[index].Length > 1
            && segments[index][0] == 'v'
            && segments[index].Skip(1).All(char.IsDigit))
        {
            index++;
        }

        if (index >= segments.Length)
        {
            return false;
        }

        var publicIdSegments = segments[index..].ToArray();
        var last = publicIdSegments[^1];
        var extension = Path.GetExtension(last);
        if (!string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension))
        {
            publicIdSegments[^1] = last[..^extension.Length];
        }

        if (publicIdSegments.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        publicId = string.Join('/', publicIdSegments);
        return !string.IsNullOrWhiteSpace(publicId);
    }

    public static bool PublicIdMatchesDeliveryUrl(string? url, string? publicId, string cloudName)
    {
        var normalizedPublicId = NormalizeOptional(publicId);
        if (normalizedPublicId is null)
        {
            return false;
        }

        return TryExtractPublicIdFromDeliveryUrl(url, cloudName, out var extracted)
            && string.Equals(extracted, normalizedPublicId, StringComparison.Ordinal);
    }

    public static bool HasFolderPrefix(string? publicId, string folder)
    {
        var normalizedPublicId = NormalizeOptional(publicId);
        var normalizedFolder = NormalizeOptional(folder);
        if (normalizedPublicId is null || normalizedFolder is null)
        {
            return false;
        }

        return normalizedPublicId.Equals(normalizedFolder, StringComparison.Ordinal)
            || normalizedPublicId.StartsWith(normalizedFolder + "/", StringComparison.Ordinal);
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
