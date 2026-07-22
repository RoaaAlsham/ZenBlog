using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class UpdateProfileCommandHandlerTests
{
    private const string CloudUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/a.png";
    private const string CloudPublicId = "zenblog/profiles/a";

    [Fact]
    public async Task Handle_UpdatesNamesAndImage_FromAuthenticatedUser()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Old",
            LastName = "Name",
            ImageUrl = null,
            ImagePublicId = null
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var sut = new UpdateProfileCommandHandler(
            userManager.Object,
            currentUser.Object,
            imageStorage.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = " New ",
                LastName = " Person ",
                ImageUrl = $" {CloudUrl} ",
                ImagePublicId = $" {CloudPublicId} "
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", user.FirstName);
        Assert.Equal("Person", user.LastName);
        Assert.Equal(CloudUrl, user.ImageUrl);
        Assert.Equal(CloudPublicId, user.ImagePublicId);
        Assert.Equal("New", result.Data!.FirstName);
        Assert.Equal(CloudPublicId, result.Data.ImagePublicId);
        imageStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ReplacingImage_DeletesOldPublicId()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/old.png",
            ImagePublicId = "zenblog/profiles/old"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/profiles/old", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UpdateProfileCommandHandler(
            userManager.Object,
            currentUser.Object,
            imageStorage.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = "Zen",
                LastName = "User",
                ImageUrl = CloudUrl,
                ImagePublicId = CloudPublicId
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CloudPublicId, user.ImagePublicId);
        imageStorage.Verify(
            x => x.DeleteAsync("zenblog/profiles/old", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BlankImage_ClearsAndDeletesOldPublicId()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/old.png",
            ImagePublicId = "zenblog/profiles/old"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/profiles/old", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UpdateProfileCommandHandler(
            userManager.Object,
            currentUser.Object,
            imageStorage.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = "Zen",
                LastName = "User",
                ImageUrl = "   ",
                ImagePublicId = null
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.ImageUrl);
        Assert.Null(user.ImagePublicId);
        imageStorage.Verify(
            x => x.DeleteAsync("zenblog/profiles/old", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateFails_DoesNotDeleteOldPublicId()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/old.png",
            ImagePublicId = "zenblog/profiles/old"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        var sut = new UpdateProfileCommandHandler(
            userManager.Object,
            currentUser.Object,
            imageStorage.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = "Zen",
                LastName = "User",
                ImageUrl = CloudUrl,
                ImagePublicId = CloudPublicId
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        imageStorage.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
