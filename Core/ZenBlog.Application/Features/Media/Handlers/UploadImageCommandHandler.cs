using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Features.Media.Commands;
using ZenBlog.Application.Features.Media.Results;

namespace ZenBlog.Application.Features.Media.Handlers;

public sealed class UploadImageCommandHandler(
    IImageStorageService imageStorage,
    ICurrentUserService currentUser)
    : IRequestHandler<UploadImageCommand, BaseResult<UploadImageResult>>
{
    public async Task<BaseResult<UploadImageResult>> Handle(
        UploadImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<UploadImageResult>.Unauthorized("You are not authenticated.");
        }

        try
        {
            var stored = await imageStorage.UploadAsync(
                request.Content,
                request.FileName,
                request.ContentType,
                ImageUploadLimits.FolderFor(request.Purpose),
                cancellationToken);

            return BaseResult<UploadImageResult>.Success(
                new UploadImageResult(stored.Url, stored.PublicId));
        }
        catch (Exception ex)
        {
            return BaseResult<UploadImageResult>.Failure($"Image upload failed: {ex.Message}");
        }
    }
}
