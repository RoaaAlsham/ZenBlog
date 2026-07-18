using MediatR;
using Microsoft.AspNetCore.Identity;
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
        UserManager<AppUser> userManager,
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
                var caller = await userManager.FindByIdAsync(currentUser.UserId);
                var roles = caller is null ? [] : await userManager.GetRolesAsync(caller);
                var isAdmin = roles.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));
                if (!isAdmin)
                {
                    return BaseResult<bool>.Forbidden("You are not authorized to delete this blog.");
                }
            }

            if (!string.IsNullOrWhiteSpace(blog.CoverImagePublicId))
            {
                await imageStorage.DeleteAsync(blog.CoverImagePublicId, cancellationToken);
            }

            await repo.DeleteAsync(blog);
            var saved = await unitOfWork.SaveChangesAsync();
            return saved ? BaseResult<bool>.Success(true) : BaseResult<bool>.Failure("Failed to delete the blog.");
        }
    }
}
