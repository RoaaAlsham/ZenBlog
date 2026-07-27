using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Handlers;

public class UpdateProfileCommandHandler(
    IUserQueryService userQuery,
    IUserAccountService userAccount,
    ICurrentUserService currentUser,
    IImageStorageService imageStorage)
    : IRequestHandler<UpdateProfileCommand, BaseResult<UserProfileResult>>
{
    public async Task<BaseResult<UserProfileResult>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<UserProfileResult>.Unauthorized("You are not authenticated.");
        }

        var user = await userQuery.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return BaseResult<UserProfileResult>.NotFound("User not found.");
        }

        var newUrl = CloudinaryImageRules.NormalizeOptional(request.ImageUrl);
        var newPublicId = CloudinaryImageRules.NormalizeOptional(request.ImagePublicId);
        var oldPublicId = user.ImagePublicId;

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.ImageUrl = newUrl;
        user.ImagePublicId = newPublicId;

        var result = await userAccount.UpdateAsync(user, cancellationToken);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors);
            return BaseResult<UserProfileResult>.Failure(errors);
        }

        // Delete the previous Cloudinary asset only after Identity update succeeds.
        if (!string.Equals(oldPublicId, newPublicId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(oldPublicId))
        {
            await imageStorage.DeleteAsync(oldPublicId, cancellationToken);
        }

        return BaseResult<UserProfileResult>.Success(GetCurrentUserQueryHandler.ToProfileResult(user));
    }
}
