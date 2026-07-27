// Queries/GetCommentsByBlogIdQuery.cs
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Comments.Results;

namespace ZenBlog.Application.Features.Comments.Queries
{
    public record GetCommentsByBlogIdQuery(
        Guid BlogId,
        int? Page = null,
        int? PageSize = null)
        : IRequest<BaseResult<PagedResult<CommentResult>>>;
}
