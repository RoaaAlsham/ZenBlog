using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class RemoveBlogCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsFailureAndDoesNotDelete()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);

        var command = new RemoveBlogCommand(Guid.NewGuid());
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Title",
            Description = "Description",
            CategoryId = Guid.NewGuid(),
            UserId = "blog-owner-id"
        };
        var callerId = "different-user-id";

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(callerId);
        currentUser.SetupGet(x => x.IsAdmin).Returns(false);

        var userQuery = new Mock<IUserQueryService>(MockBehavior.Loose);

        var sut = new RemoveBlogCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            imageStorage.Object,
            userQuery.Object,
            MonitoringMocks.ActivityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("You are not authorized to delete this blog.", Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.DeleteAsync(It.IsAny<Blog>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_SaveFails_DoesNotDeleteCloudinaryAsset()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);

        var ownerId = "owner-id";
        var command = new RemoveBlogCommand(Guid.NewGuid());
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Title",
            Description = "Description",
            CategoryId = Guid.NewGuid(),
            UserId = ownerId,
            CoverImagePublicId = "zenblog/covers/x"
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        repository.Setup(x => x.DeleteAsync(blog)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        var userQuery = new Mock<IUserQueryService>(MockBehavior.Loose);

        var sut = new RemoveBlogCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            imageStorage.Object,
            userQuery.Object,
            MonitoringMocks.ActivityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        imageStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SaveSucceeds_DeletesCloudinaryAssetAfterCommit()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);

        var ownerId = "owner-id";
        var command = new RemoveBlogCommand(Guid.NewGuid());
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Title",
            Description = "Description",
            CategoryId = Guid.NewGuid(),
            UserId = ownerId,
            CoverImagePublicId = "zenblog/covers/x"
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        repository.Setup(x => x.DeleteAsync(blog)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/covers/x", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var userQuery = new Mock<IUserQueryService>(MockBehavior.Loose);

        var sut = new RemoveBlogCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            imageStorage.Object,
            userQuery.Object,
            MonitoringMocks.ActivityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        imageStorage.Verify(
            x => x.DeleteAsync("zenblog/covers/x", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
