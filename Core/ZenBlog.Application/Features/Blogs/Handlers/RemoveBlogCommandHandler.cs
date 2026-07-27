using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class RemoveBlogCommandHandler(
        IRepository<Blog> repo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRoleChecker roleChecker,
        IImageStorageService imageStorage) : IRequestHandler<RemoveBlogCommand, BaseResult<bool>>
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
            if (!isOwner)
            {
                if (!await roleChecker.IsInRoleAsync(currentUser.UserId, "Admin", cancellationToken))
                {
                    return BaseResult<bool>.Forbidden("You are not authorized to delete this blog.");
                }
            }

            var coverPublicId = blog.CoverImagePublicId;

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

            return BaseResult<bool>.Success(true);
        }
    }
}
