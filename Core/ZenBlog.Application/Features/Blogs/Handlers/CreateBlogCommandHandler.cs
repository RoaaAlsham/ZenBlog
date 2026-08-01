using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class CreateBlogCommandHandler(
        IRepository<Domain.Entities.Blog> repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        IActivityLogger activityLogger) : IRequestHandler<CreateBlogCommand, BaseResult<CreateBlogResult>>
    {
        public async Task<BaseResult<CreateBlogResult>> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
        {
            request.CoverImageUrl = CloudinaryImageRules.NormalizeOptional(request.CoverImageUrl);
            request.CoverImagePublicId = CloudinaryImageRules.NormalizeOptional(request.CoverImagePublicId);

            var blog = mapper.Map<Blog>(request);
            // Ignore whatever UserId the client sent in the body - the owner of a new
            // blog is always the authenticated caller, taken from their validated token.
            blog.UserId = currentUser.UserId!;
            await repository.CreateAsync(blog);
            var saved = await unitOfWork.SaveChangesAsync();
            if (!saved)
            {
                return BaseResult<CreateBlogResult>.Failure("Failed to create blog.");
            }

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.BlogCreated,
                $"Created blog '{blog.Title}'",
                actorId,
                actorName,
                nameof(Blog),
                blog.Id.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<CreateBlogResult>.Success(new CreateBlogResult(blog.Id, blog.Title));
        }
    }
}
