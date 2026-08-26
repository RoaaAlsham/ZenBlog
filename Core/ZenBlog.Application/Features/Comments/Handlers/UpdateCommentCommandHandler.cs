using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers
{
    public class UpdateCommentCommandHandler(
        IRepository<Comment> repo,
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        IActivityLogger activityLogger)
        : IRequestHandler<UpdateCommentCommand, BaseResult<CommentResult>>
    {
        public async Task<BaseResult<CommentResult>> Handle(
            UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await repo.GetSingleWithIncludesAsync(
                c => c.Id == request.Id,
                cancellationToken,
                c => c.User,
                c => c.Replies);

            if (comment == null)
                return BaseResult<CommentResult>.NotFound($"Comment with id {request.Id} not found.");

            // Same authz as delete: only the author or an Admin may edit a comment.
            if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return BaseResult<CommentResult>.Unauthorized("You are not authorized to update this comment.");
            }

            var isOwner = comment.UserId == currentUser.UserId;
            if (!isOwner && !currentUser.IsAdmin)
            {
                return BaseResult<CommentResult>.Forbidden("You are not authorized to update this comment.");
            }

            mapper.Map(request, comment);
            await repo.UpdateAsync(comment);
            var saved = await uow.SaveChangesAsync();

            if (!saved)
                return BaseResult<CommentResult>.Failure("Failed to update comment.");

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.CommentUpdated,
                $"Updated comment {comment.Id}",
                actorId,
                actorName,
                nameof(Comment),
                comment.Id.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<CommentResult>.Success(mapper.Map<CommentResult>(comment));
        }
    }
}
