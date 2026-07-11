using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class GetBlogByIdQueryHandler(IRepository<Blog> repo, IMapper mapper) : IRequestHandler<GetBlogByIdQuery, BaseResult<GetBlogsQueryResult>>
    {
        public async Task<BaseResult<GetBlogsQueryResult>> Handle(
    GetBlogByIdQuery request, CancellationToken cancellationToken)
        {
            var blog = await repo.GetSingleWithIncludesAsync(
                b => b.Id == request.Id,   // filter
                cancellationToken,          // ct
                b => b.Category,
                b => b.User
            );

            if (blog == null)
                return BaseResult<GetBlogsQueryResult>.NotFound(
                    $"Blog with id {request.Id} not found.");

            return BaseResult<GetBlogsQueryResult>.Success(
                mapper.Map<GetBlogsQueryResult>(blog));
        }
    }
}
