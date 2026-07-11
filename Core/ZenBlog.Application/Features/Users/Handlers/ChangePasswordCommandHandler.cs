using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class ChangePasswordCommandHandler(
    UserManager<AppUser> userManager,
    ICurrentUserService currentUser)
    : IRequestHandler<ChangePasswordCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<bool>.Unauthorized("You are not authenticated.");
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user is null)
        {
            return BaseResult<bool>.NotFound("User not found.");
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new Error
            {
                PropertyName = e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    ? "NewPassword"
                    : null,
                ErrorMessage = e.Description
            });
            return BaseResult<bool>.Failure(errors);
        }

        return BaseResult<bool>.Success(true);
    }
}
