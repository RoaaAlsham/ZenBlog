using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class UpdateBlogCommandHandler(
        IRepository<Blog> repo,
        IMapper mapper,
        IUnitOfWork uow,
        IImageStorageService imageStorage,
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        IActivityLogger activityLogger)
        : IRequestHandler<UpdateBlogCommand, BaseResult<GetBlogsQueryResult>>
    {
        public async Task<BaseResult<GetBlogsQueryResult>> Handle(
            UpdateBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (blog == null)
                return BaseResult<GetBlogsQueryResult>.NotFound($"Blog with id {request.Id} not found.");

            // Same authz as delete: only the author or an Admin may update a blog.
            if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return BaseResult<GetBlogsQueryResult>.Unauthorized("You are not authorized to update this blog.");
            }

            var isOwner = blog.UserId == currentUser.UserId;
            if (!isOwner && !currentUser.IsAdmin)
            {
                return BaseResult<GetBlogsQueryResult>.Forbidden("You are not authorized to update this blog.");
            }

            var newUrl = CloudinaryImageRules.NormalizeOptional(request.CoverImageUrl);
            var newPublicId = CloudinaryImageRules.NormalizeOptional(request.CoverImagePublicId);
            var oldPublicId = blog.CoverImagePublicId;

            request.CoverImageUrl = newUrl;
            request.CoverImagePublicId = newPublicId;
            mapper.Map(request, blog);
            await repo.UpdateAsync(blog);
            var saved = await uow.SaveChangesAsync();

            if (!saved)
                return BaseResult<GetBlogsQueryResult>.Failure("Failed to update blog.");

            // Delete the previous Cloudinary asset only after DB commit so a failed
            // save cannot leave the blog pointing at a deleted cover image.
            if (!string.Equals(oldPublicId, newPublicId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(oldPublicId))
            {
                await imageStorage.DeleteAsync(oldPublicId, cancellationToken);
            }

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.BlogUpdated,
                $"Updated blog '{blog.Title}'",
                actorId,
                actorName,
                nameof(Blog),
                blog.Id.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<GetBlogsQueryResult>.Success(mapper.Map<GetBlogsQueryResult>(blog));
        }
    }
}
