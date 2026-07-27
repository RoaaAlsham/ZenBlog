using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Blogs.Results;

namespace ZenBlog.Application.Features.Blogs.Queries
{
    public record GetBlogsQuery(
        int? Page = null,
        int? PageSize = null,
        string? Search = null,
        Guid? CategoryId = null)
        : IRequest<BaseResult<PagedResult<GetBlogsQueryResult>>>;
}
