using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class GetPublicUserByUsernameQueryHandlerTests
{
    [Fact]
    public async Task Handle_UnknownUsername_ReturnsNotFound()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        userQuery
            .Setup(x => x.FindByUserNameAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var sut = new GetPublicUserByUsernameQueryHandler(userQuery.Object);
        var result = await sut.Handle(new GetPublicUserByUsernameQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User not found.", Assert.Single(result.Errors).ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_KnownUsername_ReturnsPublicProfileWithoutEmail()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "secret@example.com",
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = null
        };
        userQuery
            .Setup(x => x.FindByUserNameAsync("zenuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new GetPublicUserByUsernameQueryHandler(userQuery.Object);
        var result = await sut.Handle(new GetPublicUserByUsernameQuery("zenuser"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.UserName, result.Data.Username);
        Assert.Equal(user.FirstName, result.Data.FirstName);
        Assert.Equal(user.LastName, result.Data.LastName);
        Assert.DoesNotContain("secret@example.com", result.Data.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
