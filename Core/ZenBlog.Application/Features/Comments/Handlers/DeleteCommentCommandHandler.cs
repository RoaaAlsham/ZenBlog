using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers;

public class DeleteCommentCommandHandler(
    IRepository<Comment> repo,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IRoleChecker roleChecker)
    : IRequestHandler<RemoveCommentCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(
        RemoveCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (comment is null)
        {
            return BaseResult<bool>.NotFound($"Comment with id {request.Id} not found.");
        }

        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<bool>.Unauthorized("You are not authorized to delete this comment.");
        }

        var isOwner = comment.UserId == currentUser.UserId;
        if (!isOwner)
        {
            if (!await roleChecker.IsInRoleAsync(currentUser.UserId, "Admin", cancellationToken))
            {
                return BaseResult<bool>.Forbidden("You are not authorized to delete this comment.");
            }
        }

        await CommentSubtreeDelete.DeleteAsync(request.Id, repo, cancellationToken);
        var saved = await uow.SaveChangesAsync();

        return saved
            ? BaseResult<bool>.Success(true)
            : BaseResult<bool>.Failure("Failed to delete comment.");
    }
}
