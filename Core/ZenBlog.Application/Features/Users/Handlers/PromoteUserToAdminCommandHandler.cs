using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Commands;

namespace ZenBlog.Application.Features.Users.Handlers;

public class PromoteUserToAdminCommandHandler(
    IUserQueryService userQuery,
    IRoleChecker roleChecker,
    ICurrentUserService currentUser)
    : IRequestHandler<PromoteUserToAdminCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(
        PromoteUserToAdminCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<bool>.Unauthorized("You must be signed in to promote a user.");
        }

        var isAdmin = await roleChecker.IsInRoleAsync(
            currentUser.UserId,
            UserAccountHardDelete.AdminRoleName,
            cancellationToken);

        if (!isAdmin)
        {
            return BaseResult<bool>.Forbidden("Only administrators can promote users to admin.");
        }

        var target = await userQuery.FindByIdAsync(request.Id, cancellationToken);
        if (target is null)
        {
            return BaseResult<bool>.NotFound($"User with id {request.Id} not found.");
        }

        var targetIsAdmin = await roleChecker.IsInRoleAsync(
            request.Id,
            UserAccountHardDelete.AdminRoleName,
            cancellationToken);

        if (targetIsAdmin)
        {
            return BaseResult<bool>.Success(true);
        }

        var addResult = await roleChecker.AddToRoleAsync(
            request.Id,
            UserAccountHardDelete.AdminRoleName,
            cancellationToken);

        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors);
            return BaseResult<bool>.Failure(errors);
        }

        return BaseResult<bool>.Success(true);
    }
}
