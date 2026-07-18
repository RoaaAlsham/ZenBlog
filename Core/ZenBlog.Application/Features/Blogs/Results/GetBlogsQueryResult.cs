using ZenBlog.Application.Base;
using ZenBlog.Application.DTOs;
using ZenBlog.Application.DTOs.ZenBlog.Application.DTOs;

namespace ZenBlog.Application.Features.Blogs.Results
{
    public class GetBlogsQueryResult: BaseDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? CoverImagePublicId { get; set; }
        public Guid CategoryId { get; set; }
        public string UserId { get; set; }

        public CategoryDto Category { get; set; }
        public UserDto User { get; set; }
    }
}
