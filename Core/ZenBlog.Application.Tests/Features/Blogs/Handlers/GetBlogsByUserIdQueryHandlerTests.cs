using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Domain.Entities;
using System.Linq.Expressions;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class GetBlogsByUserIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_EmptyUserId_ReturnsEmptyList()
    {
        var repo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        var sut = new GetBlogsByUserIdQueryHandler(repo.Object, mapper.Object);
        var result = await sut.Handle(new GetBlogsByUserIdQuery("  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task Handle_FiltersByUserId_AndMapsResults()
    {
        var repo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var blogs = new List<Blog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Mine",
                Description = "Desc",
                UserId = "u1",
                CategoryId = Guid.NewGuid()
            }
        };
        var mapped = new List<GetBlogsQueryResult>
        {
            new()
            {
                Id = blogs[0].Id,
                Title = "Mine",
                Description = "Desc",
                UserId = "u1",
                CategoryId = blogs[0].CategoryId,
                Category = null!
            }
        };

        repo
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<Expression<Func<Blog, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Blog, object>>[]>()))
            .ReturnsAsync(blogs);
        mapper
            .Setup(x => x.Map<IEnumerable<GetBlogsQueryResult>>(blogs))
            .Returns(mapped);

        var sut = new GetBlogsByUserIdQueryHandler(repo.Object, mapper.Object);
        var result = await sut.Handle(new GetBlogsByUserIdQuery("u1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Mine", result.Data!.First().Title);
    }
}
