using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Blogs.Results;

namespace ZenBlog.Application.Features.Blogs.Commands
{
    public class UpdateBlogCommand : IRequest<BaseResult<GetBlogsQueryResult>>
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public Guid CategoryId { get; set; }
    }
}
