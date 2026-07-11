using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Blogs.Results;

namespace ZenBlog.Application.Features.Blogs.Queries;

public sealed record GetBlogsByUserIdQuery(string UserId)
    : IRequest<BaseResult<IEnumerable<GetBlogsQueryResult>>>;
