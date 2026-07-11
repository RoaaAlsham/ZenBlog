using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class GetBlogsQueryHandler(IRepository<Blog> repository, IMapper mapper) : IRequestHandler<GetBlogsQuery, BaseResult<List<GetBlogsQueryResult>>>
    {
        public async Task<BaseResult<List<GetBlogsQueryResult>>> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
        {
            var values = await repository.GetAllWithIncludesAsync(
    cancellationToken,
    b => b.Category,
    b => b.User);
            var blogs = mapper.Map<List<GetBlogsQueryResult>>(values);
            return BaseResult<List<GetBlogsQueryResult>>.Success(blogs);
        
        }
    }
}
