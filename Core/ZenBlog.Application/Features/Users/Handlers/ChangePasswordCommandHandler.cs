using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Commands;

namespace ZenBlog.Application.Features.Users.Handlers;

public class ChangePasswordCommandHandler(
    IUserQueryService userQuery,
    IUserAccountService userAccount,
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

        var user = await userQuery.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return BaseResult<bool>.NotFound("User not found.");
        }

        var result = await userAccount.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new Error
            {
                PropertyName = e.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    ? "NewPassword"
                    : null,
                ErrorMessage = e
            });
            return BaseResult<bool>.Failure(errors);
        }

        return BaseResult<bool>.Success(true);
    }
}
