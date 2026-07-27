using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Queries;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Handlers
{
    public class GetCommentByIdQueryHandler(IRepository<Comment> repo, IMapper mapper)
        : IRequestHandler<GetCommentByIdQuery, BaseResult<CommentResult>>
    {
        public async Task<BaseResult<CommentResult>> Handle(
            GetCommentByIdQuery request, CancellationToken cancellationToken)
        {
            var comment = await repo.GetSingleWithIncludePathsAsync(
                c => c.Id == request.Id,
                cancellationToken,
                "User",
                "Replies",
                "Replies.User");

            if (comment == null)
                return BaseResult<CommentResult>.NotFound($"Comment with id {request.Id} not found.");

            return BaseResult<CommentResult>.Success(mapper.Map<CommentResult>(comment));
        }
    }
}
