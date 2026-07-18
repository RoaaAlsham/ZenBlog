using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Models;

namespace ZenBlog.Infrastructure.Media;

public sealed class CloudinaryImageStorageService : IImageStorageService
{
    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _cloudinary;

    public CloudinaryImageStorageService(IOptions<CloudinarySettings> options)
    {
        _settings = options.Value;
        _cloudinary = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.Value.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        if (string.IsNullOrWhiteSpace(result.SecureUrl?.ToString())
            || string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new InvalidOperationException("Cloudinary upload returned an incomplete result.");
        }

        return new StoredImage(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var result = await _cloudinary.Value.DestroyAsync(deletionParams);
        if (result.Error is not null
            && !result.Error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
        }
    }

    private Cloudinary CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException(
                "CloudinarySettings (CloudName, ApiKey, ApiSecret) must be configured.");
        }

        var cloudinary = new Cloudinary(new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret));
        cloudinary.Api.Secure = true;
        return cloudinary;
    }
}
