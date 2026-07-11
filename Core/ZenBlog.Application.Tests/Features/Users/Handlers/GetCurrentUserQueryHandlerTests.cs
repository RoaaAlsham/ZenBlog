using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = new GetCurrentUserQueryHandler(userManager.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("missing");
        userManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((AppUser?)null);

        var sut = new GetCurrentUserQueryHandler(userManager.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User not found.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsProfile()
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
            ImageUrl = "https://cdn.example.com/a.png"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var sut = new GetCurrentUserQueryHandler(userManager.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.UserName, result.Data.Username);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(user.FirstName, result.Data.FirstName);
        Assert.Equal(user.LastName, result.Data.LastName);
        Assert.Equal(user.ImageUrl, result.Data.ImageUrl);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
