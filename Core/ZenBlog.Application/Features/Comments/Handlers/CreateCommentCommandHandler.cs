using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers
{
    public class CreateCommentCommandHandler(
        IRepository<Comment> repo,
        IRepository<Blog> blogRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser) :
        IRequestHandler<CreateCommentCommand, BaseResult<CreateCommentResult>>
    {
        public async Task<BaseResult<CreateCommentResult>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            var blog = await blogRepo.GetByIdAsync(request.BlogId, cancellationToken);
            if (blog is null)
            {
                return BaseResult<CreateCommentResult>.NotFound($"Blog with id {request.BlogId} not found.");
            }

            // Parent must exist on the same blog so replies cannot attach across posts.
            if (request.ParentCommentId is Guid parentId)
            {
                var parent = await repo.GetByIdAsync(parentId, cancellationToken);
                if (parent is null)
                {
                    return BaseResult<CreateCommentResult>.NotFound($"Parent comment with id {parentId} not found.");
                }

                if (parent.BlogId != request.BlogId)
                {
                    return BaseResult<CreateCommentResult>.Failure("Parent comment does not belong to this blog.");
                }
            }

            var comment = mapper.Map<Comment>(request);
            // Same rule as blogs: the comment author is the authenticated caller,
            // never whatever UserId the client happened to put in the request body.
            comment.UserId = currentUser.UserId!;
            await repo.CreateAsync(comment);
            var saved = await unitOfWork.SaveChangesAsync();

            if (!saved)
            {
                return BaseResult<CreateCommentResult>.Failure("Failed to create comment.");
            }
            return BaseResult<CreateCommentResult>.Success(new CreateCommentResult(comment.Id, comment.Body,
                comment.BlogId, comment.ParentCommentId));
        }
    }
}
