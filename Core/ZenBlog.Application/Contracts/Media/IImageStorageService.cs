namespace ZenBlog.Application.Contracts.Media;

public interface IImageStorageService
{
    Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the asset identified by <paramref name="publicId"/>.
    /// Missing assets are treated as success.
    /// </summary>
    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

public sealed record StoredImage(string Url, string PublicId);
