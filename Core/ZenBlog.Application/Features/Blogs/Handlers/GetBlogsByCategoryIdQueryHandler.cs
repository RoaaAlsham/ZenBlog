using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class GetBlogsByCategoryIdQueryHandler(IRepository<Blog> repo, IMapper mapper) : IRequestHandler<GetBlogsByCategoryIdQuery, BaseResult<IEnumerable<GetBlogsQueryResult>>>
    {
        public async Task<BaseResult<IEnumerable<GetBlogsQueryResult>>> Handle(GetBlogsByCategoryIdQuery request, CancellationToken cancellationToken)
        {
            var blogs = await repo.GetAllWithIncludesAsync(
    b => b.CategoryId == request.CategoryId,  // filter
    cancellationToken,                         // ct
    b => b.Category,
    b => b.User
);

            return BaseResult<IEnumerable<GetBlogsQueryResult>>
                .Success(mapper.Map<IEnumerable<GetBlogsQueryResult>>(blogs));

        }
    }
}
