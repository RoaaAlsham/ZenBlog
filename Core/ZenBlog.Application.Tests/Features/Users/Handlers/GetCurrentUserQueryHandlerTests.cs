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
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = new GetCurrentUserQueryHandler(userQuery.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("missing");
        userQuery
            .Setup(x => x.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var sut = new GetCurrentUserQueryHandler(userQuery.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User not found.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsProfile()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
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
        // Roles come from the gateway, not from this service's own tables.
        currentUser.SetupGet(x => x.Roles).Returns(["Admin", "Editor"]);
        userQuery
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new GetCurrentUserQueryHandler(userQuery.Object, currentUser.Object);
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.UserName, result.Data.Username);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(user.FirstName, result.Data.FirstName);
        Assert.Equal(user.LastName, result.Data.LastName);
        Assert.Equal(user.ImageUrl, result.Data.ImageUrl);
        Assert.Equal(user.ImagePublicId, result.Data.ImagePublicId);
        // Relayed verbatim: this is how the browser learns whether it is an admin.
        Assert.Equal(["Admin", "Editor"], result.Data.Roles);
    }
}
