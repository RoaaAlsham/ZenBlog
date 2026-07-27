using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class GetBlogsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ClampsPageSizeAndReturnsPagedResult()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        var blogs = new List<Blog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "One",
                Description = "D1",
                CategoryId = Guid.NewGuid(),
                UserId = "u1"
            }
        };

        int capturedPage = 0;
        int capturedSize = 0;
        repository
            .Setup(x => x.GetPagedWithIncludePathsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Blog, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .Callback<System.Linq.Expressions.Expression<Func<Blog, bool>>, int, int, CancellationToken, string[]>(
                (_, page, size, _, _) =>
                {
                    capturedPage = page;
                    capturedSize = size;
                })
            .ReturnsAsync((blogs, 25));

        mapper
            .Setup(x => x.Map<List<GetBlogsQueryResult>>(It.IsAny<List<Blog>>()))
            .Returns(
            [
                new GetBlogsQueryResult
                {
                    Id = blogs[0].Id,
                    Title = "One",
                    Description = "D1",
                    CategoryId = blogs[0].CategoryId,
                    UserId = "u1"
                }
            ]);

        var sut = new GetBlogsQueryHandler(repository.Object, mapper.Object);
        var result = await sut.Handle(
            new GetBlogsQuery(Page: 0, PageSize: 999),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, capturedPage);
        Assert.Equal(50, capturedSize);
        Assert.Equal(25, result.Data!.TotalCount);
        Assert.Equal(1, result.Data.Page);
        Assert.Equal(50, result.Data.PageSize);
        Assert.Single(result.Data.Items);
    }
}
