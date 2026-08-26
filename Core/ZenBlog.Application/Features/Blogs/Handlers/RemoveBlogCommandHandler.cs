using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class RemoveBlogCommandHandler(
        IRepository<Blog> repo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IImageStorageService imageStorage,
        IUserQueryService userQuery,
        IActivityLogger activityLogger) : IRequestHandler<RemoveBlogCommand, BaseResult<bool>>
    {
        public async Task<BaseResult<bool>> Handle(RemoveBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (blog == null)
            {
                return BaseResult<bool>.NotFound($"Blog with id {request.Id} not found.");
            }

            if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return BaseResult<bool>.Unauthorized("You are not authorized to delete this blog.");
            }

            var isOwner = blog.UserId == currentUser.UserId;
            if (!isOwner && !currentUser.IsAdmin)
            {
                return BaseResult<bool>.Forbidden("You are not authorized to delete this blog.");
            }

            var coverPublicId = blog.CoverImagePublicId;
            var blogTitle = blog.Title;
            var blogId = blog.Id;

            await repo.DeleteAsync(blog);
            var saved = await unitOfWork.SaveChangesAsync();
            if (!saved)
            {
                return BaseResult<bool>.Failure("Failed to delete the blog.");
            }

            // Delete Cloudinary asset only after DB commit so a failed save
            // cannot leave the cover image gone while the blog row remains.
            if (!string.IsNullOrWhiteSpace(coverPublicId))
            {
                await imageStorage.DeleteAsync(coverPublicId, cancellationToken);
            }

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.BlogDeleted,
                $"Deleted blog '{blogTitle}'",
                actorId,
                actorName,
                nameof(Blog),
                blogId.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<bool>.Success(true);
        }
    }
}
