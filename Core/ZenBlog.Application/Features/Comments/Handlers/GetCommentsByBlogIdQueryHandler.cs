// Handlers/GetCommentsByBlogIdQueryHandler.cs
using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Queries;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers
{
    public class GetCommentsByBlogIdQueryHandler(IRepository<Comment> repo, IMapper mapper)
        : IRequestHandler<GetCommentsByBlogIdQuery, BaseResult<PagedResult<CommentResult>>>
    {
        public async Task<BaseResult<PagedResult<CommentResult>>> Handle(
            GetCommentsByBlogIdQuery request, CancellationToken cancellationToken)
        {
            var (page, pageSize) = Paging.Normalize(
                request.Page,
                request.PageSize,
                Paging.DefaultCommentsPageSize);

            // Top-level comments only; one reply level with authors (Replies.User).
            var (items, totalCount) = await repo.GetPagedWithIncludePathsAsync(
                c => c.BlogId == request.BlogId && c.ParentCommentId == null,
                page,
                pageSize,
                cancellationToken,
                "User",
                "Replies",
                "Replies.User");

            var mapped = mapper.Map<List<CommentResult>>(items);
            return BaseResult<PagedResult<CommentResult>>.Success(
                PagedResult<CommentResult>.Create(mapped, page, pageSize, totalCount));
        }
    }
}
