using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Application.Features.Media;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class UpdateBlogCommandHandler(
        IRepository<Blog> repo,
        IMapper mapper,
        IUnitOfWork uow,
        IImageStorageService imageStorage)
        : IRequestHandler<UpdateBlogCommand, BaseResult<GetBlogsQueryResult>>
    {
        public async Task<BaseResult<GetBlogsQueryResult>> Handle(
            UpdateBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (blog == null)
                return BaseResult<GetBlogsQueryResult>.NotFound($"Blog with id {request.Id} not found.");

            var newUrl = CloudinaryImageRules.NormalizeOptional(request.CoverImageUrl);
            var newPublicId = CloudinaryImageRules.NormalizeOptional(request.CoverImagePublicId);
            var oldPublicId = blog.CoverImagePublicId;

            if (!string.Equals(oldPublicId, newPublicId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(oldPublicId))
            {
                await imageStorage.DeleteAsync(oldPublicId, cancellationToken);
            }

            request.CoverImageUrl = newUrl;
            request.CoverImagePublicId = newPublicId;
            mapper.Map(request, blog);
            await repo.UpdateAsync(blog);
            var saved = await uow.SaveChangesAsync();

            if (!saved)
                return BaseResult<GetBlogsQueryResult>.Failure("Failed to update blog.");

            return BaseResult<GetBlogsQueryResult>.Success(mapper.Map<GetBlogsQueryResult>(blog));
        }
    }
}
