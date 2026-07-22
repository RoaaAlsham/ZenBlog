using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers
{
    public class UpdateCommentCommandHandler(
        IRepository<Comment> repo,
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUserService currentUser,
        UserManager<AppUser> userManager)
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
            if (!isOwner)
            {
                var caller = await userManager.FindByIdAsync(currentUser.UserId);
                var roles = caller is null ? [] : await userManager.GetRolesAsync(caller);
                var isAdmin = roles.Any(role =>
                    string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));
                if (!isAdmin)
                {
                    return BaseResult<CommentResult>.Forbidden("You are not authorized to update this comment.");
                }
            }

            mapper.Map(request, comment);
            await repo.UpdateAsync(comment);
            var saved = await uow.SaveChangesAsync();

            if (!saved)
                return BaseResult<CommentResult>.Failure("Failed to update comment.");

            return BaseResult<CommentResult>.Success(mapper.Map<CommentResult>(comment));
        }
    }
}
