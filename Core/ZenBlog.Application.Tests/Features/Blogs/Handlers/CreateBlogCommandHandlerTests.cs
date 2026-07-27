using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class CreateBlogCommandHandlerTests
{
    [Fact]
    public async Task Handle_AlwaysUsesAuthenticatedUserId_FromJwt()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var authenticatedUserId = "jwt-user-id";
        var command = new CreateBlogCommand
        {
            Title = "Security test title",
            Description = "Security test description",
            CoverImageUrl = null,
            CoverImagePublicId = null,
            CategoryId = Guid.NewGuid()
        };

        var mappedBlog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            CategoryId = command.CategoryId,
            UserId = "mapper-placeholder"
        };

        Blog? createdEntity = null;

        mapper
            .Setup(x => x.Map<Blog>(command))
            .Returns(mappedBlog);
        currentUser
            .SetupGet(x => x.UserId)
            .Returns(authenticatedUserId);
        repository
            .Setup(x => x.CreateAsync(It.IsAny<Blog>()))
            .Callback<Blog>(blog => createdEntity = blog)
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var sut = new CreateBlogCommandHandler(
            repository.Object,
            mapper.Object,
            unitOfWork.Object,
            currentUser.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var data = Assert.IsType<ZenBlog.Application.Features.Blogs.Results.CreateBlogResult>(result.Data);
        Assert.Equal(mappedBlog.Id, data.Id);
        Assert.Equal(mappedBlog.Title, data.Title);

        var persisted = Assert.IsType<Blog>(createdEntity);
        Assert.Equal(authenticatedUserId, persisted.UserId);
    }
}
