using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Blogs.Handlers
{
    public class GetBlogsQueryHandler(IRepository<Blog> repository, IMapper mapper)
        : IRequestHandler<GetBlogsQuery, BaseResult<PagedResult<GetBlogsQueryResult>>>
    {
        public async Task<BaseResult<PagedResult<GetBlogsQueryResult>>> Handle(
            GetBlogsQuery request,
            CancellationToken cancellationToken)
        {
            var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);

            var search = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : request.Search.Trim().ToLowerInvariant();

            var (items, totalCount) = await repository.GetPagedWithIncludePathsAsync(
                b =>
                    (!request.CategoryId.HasValue || b.CategoryId == request.CategoryId.Value)
                    && (search == null
                        || b.Title.ToLower().Contains(search)
                        || b.Description.ToLower().Contains(search)),
                page,
                pageSize,
                cancellationToken,
                "Category",
                "User");

            var mapped = mapper.Map<List<GetBlogsQueryResult>>(items);
            return BaseResult<PagedResult<GetBlogsQueryResult>>.Success(
                PagedResult<GetBlogsQueryResult>.Create(mapped, page, pageSize, totalCount));
        }
    }
}
