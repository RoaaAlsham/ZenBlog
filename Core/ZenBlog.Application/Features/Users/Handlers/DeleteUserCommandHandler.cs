using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class DeleteUserCommandHandler(
    UserManager<AppUser> userManager,
    ICurrentUserService currentUser,
    IRepository<Comment> commentRepository,
    IRepository<Blog> blogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<bool>.Unauthorized("You must be signed in to delete a user.");
        }

        var caller = await userManager.FindByIdAsync(currentUser.UserId);
        var callerRoles = caller is null ? [] : await userManager.GetRolesAsync(caller);
        var isAdmin = callerRoles.Any(role =>
            string.Equals(role, UserAccountHardDelete.AdminRoleName, StringComparison.OrdinalIgnoreCase));

        if (!isAdmin)
        {
            return BaseResult<bool>.Forbidden("Only administrators can delete user accounts.");
        }

        if (string.Equals(request.Id, currentUser.UserId, StringComparison.Ordinal))
        {
            return BaseResult<bool>.Forbidden(
                "You cannot delete your own account via admin delete. Use account self-deletion instead.");
        }

        var target = await userManager.FindByIdAsync(request.Id);
        if (target is null)
        {
            return BaseResult<bool>.NotFound($"User with id {request.Id} not found.");
        }

        var targetRoles = await userManager.GetRolesAsync(target);
        var targetIsAdmin = targetRoles.Any(role =>
            string.Equals(role, UserAccountHardDelete.AdminRoleName, StringComparison.OrdinalIgnoreCase));

        if (targetIsAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(UserAccountHardDelete.AdminRoleName);
            if (admins.Count <= 1)
            {
                return BaseResult<bool>.Forbidden(
                    "Cannot delete the last administrator account.");
            }
        }

        await UserAccountHardDelete.PurgeContentAsync(
            target.Id,
            commentRepository,
            blogRepository,
            cancellationToken);
        _ = await unitOfWork.SaveChangesAsync();

        var deleteResult = await userManager.DeleteAsync(target);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return BaseResult<bool>.Failure(errors);
        }

        return BaseResult<bool>.Success(true);
    }
}
