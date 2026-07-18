using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class UpdateProfileCommandHandler(
    UserManager<AppUser> userManager,
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

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user is null)
        {
            return BaseResult<UserProfileResult>.NotFound("User not found.");
        }

        var newUrl = CloudinaryImageRules.NormalizeOptional(request.ImageUrl);
        var newPublicId = CloudinaryImageRules.NormalizeOptional(request.ImagePublicId);
        var oldPublicId = user.ImagePublicId;

        if (!string.Equals(oldPublicId, newPublicId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(oldPublicId))
        {
            await imageStorage.DeleteAsync(oldPublicId, cancellationToken);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.ImageUrl = newUrl;
        user.ImagePublicId = newPublicId;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BaseResult<UserProfileResult>.Failure(errors);
        }

        return BaseResult<UserProfileResult>.Success(GetCurrentUserQueryHandler.ToProfileResult(user));
    }
}
