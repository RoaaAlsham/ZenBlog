using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class DeleteUserCommandHandler(
    IUserQueryService userQuery,
    IUserAccountService userAccount,
    IRoleChecker roleChecker,
    ICurrentUserService currentUser,
    IRepository<Comment> commentRepository,
    IRepository<Blog> blogRepository,
    IImageStorageService imageStorage,
    IUnitOfWork unitOfWork,
    IActivityLogger activityLogger)
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

        var isAdmin = await roleChecker.IsInRoleAsync(
            currentUser.UserId,
            UserAccountHardDelete.AdminRoleName,
            cancellationToken);

        if (!isAdmin)
        {
            return BaseResult<bool>.Forbidden("Only administrators can delete user accounts.");
        }

        if (string.Equals(request.Id, currentUser.UserId, StringComparison.Ordinal))
        {
            return BaseResult<bool>.Forbidden(
                "You cannot delete your own account via admin delete. Use account self-deletion instead.");
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
            var adminCount = await roleChecker.CountUsersInRoleAsync(
                UserAccountHardDelete.AdminRoleName,
                cancellationToken);
            if (adminCount <= 1)
            {
                return BaseResult<bool>.Forbidden(
                    "Cannot delete the last administrator account.");
            }
        }

        if (!string.IsNullOrWhiteSpace(target.ImagePublicId))
        {
            await imageStorage.DeleteAsync(target.ImagePublicId, cancellationToken);
        }

        await UserAccountHardDelete.PurgeContentAsync(
            target.Id,
            commentRepository,
            blogRepository,
            imageStorage,
            cancellationToken);
        _ = await unitOfWork.SaveChangesAsync();

        var targetUserName = target.UserName;
        var targetId = target.Id;

        var deleteResult = await userAccount.DeleteAsync(target, cancellationToken);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors);
            return BaseResult<bool>.Failure(errors);
        }

        var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
        await activityLogger.LogAsync(
            ActivityActions.UserDeleted,
            $"Deleted user '{targetUserName}'",
            actorId,
            actorName,
            "User",
            targetId,
            cancellationToken: cancellationToken);

        return BaseResult<bool>.Success(true);
    }
}
