using Microsoft.AspNetCore.Identity;
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
        var userManager = CreateUserManagerMock();
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
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.ChangePasswordAsync(user, "WrongOld!", "Password123!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Incorrect password."
            }));

        var sut = new ChangePasswordCommandHandler(userManager.Object, currentUser.Object);
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
        var userManager = CreateUserManagerMock();
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
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.ChangePasswordAsync(user, "Password123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        var sut = new ChangePasswordCommandHandler(userManager.Object, currentUser.Object);
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

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
