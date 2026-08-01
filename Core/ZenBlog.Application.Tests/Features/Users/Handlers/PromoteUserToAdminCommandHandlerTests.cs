using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class PromoteUserToAdminCommandHandlerTests
{
    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand("target"), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Equal(
            "You must be signed in to promote a user.",
            Assert.Single(result.Errors).ErrorMessage);
        roleChecker.Verify(
            x => x.AddToRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var caller = CreateUser("caller-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand("target"), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "Only administrators can promote users to admin.",
            Assert.Single(result.Errors).ErrorMessage);
        roleChecker.Verify(
            x => x.AddToRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingTarget_ReturnsNotFound()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userQuery
            .Setup(x => x.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand("missing"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        roleChecker.Verify(
            x => x.AddToRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyAdmin_ReturnsSuccessWithoutAdd()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        var target = CreateUser("admin-2");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userQuery
            .Setup(x => x.FindByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        roleChecker
            .Setup(x => x.IsInRoleAsync(target.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand(target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        roleChecker.Verify(
            x => x.AddToRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AdminPromotesUser_Succeeds()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        var target = CreateUser("user-2");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userQuery
            .Setup(x => x.FindByIdAsync(caller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caller);
        userQuery
            .Setup(x => x.FindByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        roleChecker
            .Setup(x => x.IsInRoleAsync(target.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        roleChecker
            .Setup(x => x.AddToRoleAsync(
                target.Id,
                UserAccountHardDelete.AdminRoleName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand(target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        roleChecker.Verify(
            x => x.AddToRoleAsync(
                target.Id,
                UserAccountHardDelete.AdminRoleName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AddToRoleFails_ReturnsFailure()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        var target = CreateUser("user-2");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userQuery
            .Setup(x => x.FindByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        roleChecker
            .Setup(x => x.IsInRoleAsync(target.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        roleChecker
            .Setup(x => x.AddToRoleAsync(
                target.Id,
                UserAccountHardDelete.AdminRoleName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("role assignment failed"));

        var sut = CreateSut(userQuery, roleChecker, currentUser);
        var result = await sut.Handle(new PromoteUserToAdminCommand(target.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("role assignment failed", Assert.Single(result.Errors).ErrorMessage);
    }

    private static PromoteUserToAdminCommandHandler CreateSut(
        Mock<IUserQueryService> userQuery,
        Mock<IRoleChecker> roleChecker,
        Mock<ICurrentUserService> currentUser) =>
        new(
            userQuery.Object,
            roleChecker.Object,
            currentUser.Object,
            MonitoringMocks.ActivityLogger().Object);

    private static AppUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = "Test",
        LastName = "User"
    };
}
