using System.Linq.Expressions;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-caller");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut(userQuery, userAccount, roleChecker, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand("target"), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "Only administrators can delete user accounts.",
            Assert.Single(result.Errors).ErrorMessage);
        userAccount.Verify(
            x => x.DeleteAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AdminDeletingSelf_ReturnsForbidden()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(userQuery, userAccount, roleChecker, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(caller.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("self-deletion", Assert.Single(result.Errors).ErrorMessage);
        userAccount.Verify(
            x => x.DeleteAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_LastAdmin_ReturnsForbidden()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

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
        roleChecker
            .Setup(x => x.CountUsersInRoleAsync(UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(userQuery, userAccount, roleChecker, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(target.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "Cannot delete the last administrator account.",
            Assert.Single(result.Errors).ErrorMessage);
        userAccount.Verify(
            x => x.DeleteAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingTarget_ReturnsNotFound()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        roleChecker
            .Setup(x => x.IsInRoleAsync(caller.Id, UserAccountHardDelete.AdminRoleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userQuery
            .Setup(x => x.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var sut = CreateSut(userQuery, userAccount, roleChecker, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand("missing"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_AdminDeletesUser_Succeeds()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Loose);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

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
        userAccount
            .Setup(x => x.DeleteAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        commentRepo
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<Expression<Func<Comment, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Comment, object>>[]>()))
            .ReturnsAsync([]);

        blogRepo
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<Expression<Func<Blog, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Blog, object>>[]>()))
            .ReturnsAsync([]);

        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        var sut = CreateSut(userQuery, userAccount, roleChecker, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        userAccount.Verify(x => x.DeleteAsync(target, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static DeleteUserCommandHandler CreateSut(
        Mock<IUserQueryService> userQuery,
        Mock<IUserAccountService> userAccount,
        Mock<IRoleChecker> roleChecker,
        Mock<ICurrentUserService> currentUser,
        Mock<IRepository<Comment>> commentRepo,
        Mock<IRepository<Blog>> blogRepo,
        Mock<IImageStorageService> imageStorage,
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            userQuery.Object,
            userAccount.Object,
            roleChecker.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            imageStorage.Object,
            unitOfWork.Object,
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
