using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers;

public class GetBlogsByUserIdQueryHandler(IRepository<Blog> repo, IMapper mapper)
    : IRequestHandler<GetBlogsByUserIdQuery, BaseResult<IEnumerable<GetBlogsQueryResult>>>
{
    public async Task<BaseResult<IEnumerable<GetBlogsQueryResult>>> Handle(
        GetBlogsByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BaseResult<IEnumerable<GetBlogsQueryResult>>.Success([]);
        }

        var blogs = await repo.GetAllWithIncludesAsync(
            b => b.UserId == request.UserId,
            cancellationToken,
            b => b.Category,
            b => b.User);

        return BaseResult<IEnumerable<GetBlogsQueryResult>>
            .Success(mapper.Map<IEnumerable<GetBlogsQueryResult>>(blogs));
    }
}
