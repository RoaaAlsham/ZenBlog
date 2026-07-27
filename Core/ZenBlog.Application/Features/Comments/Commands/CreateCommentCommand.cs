using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Comments.Results;

namespace ZenBlog.Application.Features.Comments.Commands
{
    public class CreateCommentCommand : IRequest<BaseResult<CreateCommentResult>>
    {
        public required string Body { get; set; }
        public Guid BlogId { get; set; }
        public Guid? ParentCommentId { get; set; } // null = top-level, set = reply
    }
}
