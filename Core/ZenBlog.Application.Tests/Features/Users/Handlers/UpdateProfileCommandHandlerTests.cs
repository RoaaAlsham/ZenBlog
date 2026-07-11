using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesNamesAndImageUrl_FromAuthenticatedUser()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Old",
            LastName = "Name",
            ImageUrl = null
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var sut = new UpdateProfileCommandHandler(userManager.Object, currentUser.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = " New ",
                LastName = " Person ",
                ImageUrl = " https://cdn.example.com/avatar.png "
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", user.FirstName);
        Assert.Equal("Person", user.LastName);
        Assert.Equal("https://cdn.example.com/avatar.png", user.ImageUrl);
        Assert.Equal("New", result.Data!.FirstName);
        Assert.Equal("Person", result.Data.LastName);
    }

    [Fact]
    public async Task Handle_BlankImageUrl_ClearsImageUrl()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://cdn.example.com/old.png"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var sut = new UpdateProfileCommandHandler(userManager.Object, currentUser.Object);
        var result = await sut.Handle(
            new UpdateProfileCommand
            {
                FirstName = "Zen",
                LastName = "User",
                ImageUrl = "   "
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.ImageUrl);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
