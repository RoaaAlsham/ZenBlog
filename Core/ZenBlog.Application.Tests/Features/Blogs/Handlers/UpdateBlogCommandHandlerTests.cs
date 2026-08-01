using AutoMapper;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class UpdateBlogCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsForbiddenAndDoesNotUpdate()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);

        var command = new UpdateBlogCommand
        {
            Id = Guid.NewGuid(),
            Title = "Hacked",
            Description = "Body",
            CategoryId = Guid.NewGuid()
        };
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Title",
            Description = "Description",
            CategoryId = command.CategoryId,
            UserId = "blog-owner-id",
            CoverImagePublicId = "zenblog/covers/old"
        };
        var callerId = "different-user-id";

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(callerId);
        roleChecker
            .Setup(x => x.IsInRoleAsync(callerId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut(repository, mapper, unitOfWork, imageStorage, currentUser, roleChecker);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("You are not authorized to update this blog.", Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.UpdateAsync(It.IsAny<Blog>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
        imageStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_UpdatesSuccessfully_AndDeletesOldCoverAfterSave()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);

        var ownerId = "blog-owner-id";
        var command = new UpdateBlogCommand
        {
            Id = Guid.NewGuid(),
            Title = "New title",
            Description = "New body",
            CategoryId = Guid.NewGuid(),
            CoverImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/covers/new.png",
            CoverImagePublicId = "zenblog/covers/new"
        };
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Old",
            Description = "Old",
            CategoryId = command.CategoryId,
            UserId = ownerId,
            CoverImagePublicId = "zenblog/covers/old"
        };
        var mappedResult = new GetBlogsQueryResult
        {
            Title = command.Title,
            Description = command.Description,
            CategoryId = command.CategoryId,
            UserId = ownerId,
            CoverImagePublicId = command.CoverImagePublicId
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        mapper.Setup(x => x.Map(command, blog)).Returns(blog);
        repository.Setup(x => x.UpdateAsync(blog)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/covers/old", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapper.Setup(x => x.Map<GetBlogsQueryResult>(blog)).Returns(mappedResult);

        var sut = CreateSut(repository, mapper, unitOfWork, imageStorage, currentUser, roleChecker);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(mappedResult, result.Data);
        repository.Verify(x => x.UpdateAsync(blog), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        imageStorage.Verify(
            x => x.DeleteAsync("zenblog/covers/old", It.IsAny<CancellationToken>()),
            Times.Once);
        roleChecker.Verify(
            x => x.IsInRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SaveFails_DoesNotDeleteOldCover()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);

        var ownerId = "blog-owner-id";
        var command = new UpdateBlogCommand
        {
            Id = Guid.NewGuid(),
            Title = "New title",
            Description = "New body",
            CategoryId = Guid.NewGuid(),
            CoverImagePublicId = "zenblog/covers/new"
        };
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Old",
            Description = "Old",
            CategoryId = command.CategoryId,
            UserId = ownerId,
            CoverImagePublicId = "zenblog/covers/old"
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        mapper.Setup(x => x.Map(command, blog)).Returns(blog);
        repository.Setup(x => x.UpdateAsync(blog)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        var sut = CreateSut(repository, mapper, unitOfWork, imageStorage, currentUser, roleChecker);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Failed to update blog.", Assert.Single(result.Errors).ErrorMessage);
        imageStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AdminNonOwner_UpdatesSuccessfully()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);

        var command = new UpdateBlogCommand
        {
            Id = Guid.NewGuid(),
            Title = "Admin edit",
            Description = "Body",
            CategoryId = Guid.NewGuid()
        };
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Old",
            Description = "Old",
            CategoryId = command.CategoryId,
            UserId = "blog-owner-id"
        };
        var adminId = "admin-id";
        var mappedResult = new GetBlogsQueryResult
        {
            Title = command.Title,
            Description = command.Description,
            CategoryId = command.CategoryId,
            UserId = blog.UserId
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(adminId);
        roleChecker
            .Setup(x => x.IsInRoleAsync(adminId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mapper.Setup(x => x.Map(command, blog)).Returns(blog);
        repository.Setup(x => x.UpdateAsync(blog)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        mapper.Setup(x => x.Map<GetBlogsQueryResult>(blog)).Returns(mappedResult);

        var sut = CreateSut(repository, mapper, unitOfWork, imageStorage, currentUser, roleChecker);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(x => x.UpdateAsync(blog), Times.Once);
    }

    private static UpdateBlogCommandHandler CreateSut(
        Mock<IRepository<Blog>> repository,
        Mock<IMapper> mapper,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IImageStorageService> imageStorage,
        Mock<ICurrentUserService> currentUser,
        Mock<IRoleChecker> roleChecker) =>
        new(
            repository.Object,
            mapper.Object,
            unitOfWork.Object,
            imageStorage.Object,
            currentUser.Object,
            roleChecker.Object,
            new Mock<IUserQueryService>(MockBehavior.Loose).Object,
            MonitoringMocks.ActivityLogger().Object);
}
