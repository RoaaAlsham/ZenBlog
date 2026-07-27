using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsFailure()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userQuery
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.ChangePasswordAsync(user, "WrongOld!", "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Incorrect password."));

        var sut = new ChangePasswordCommandHandler(userQuery.Object, userAccount.Object, currentUser.Object);
        var result = await sut.Handle(
            new ChangePasswordCommand
            {
                CurrentPassword = "WrongOld!",
                NewPassword = "Password123!"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Incorrect password.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Handle_ValidChange_ReturnsSuccess()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            UserName = "zenuser",
            Email = "zen@example.com",
            FirstName = "Zen",
            LastName = "User"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userQuery
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.ChangePasswordAsync(user, "Password123!", "NewPassword123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var sut = new ChangePasswordCommandHandler(userQuery.Object, userAccount.Object, currentUser.Object);
        var result = await sut.Handle(
            new ChangePasswordCommand
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }
}
