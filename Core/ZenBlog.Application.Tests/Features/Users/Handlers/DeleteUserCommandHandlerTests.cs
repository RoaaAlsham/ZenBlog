using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-caller");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.GetRolesAsync(caller)).ReturnsAsync((IList<string>)["User"]);

        var sut = CreateSut(userManager, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand("target"), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "Only administrators can delete user accounts.",
            Assert.Single(result.Errors).ErrorMessage);
        userManager.Verify(x => x.DeleteAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminDeletingSelf_ReturnsForbidden()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager
            .Setup(x => x.GetRolesAsync(caller))
            .ReturnsAsync((IList<string>)[UserAccountHardDelete.AdminRoleName]);

        var sut = CreateSut(userManager, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(caller.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("self-deletion", Assert.Single(result.Errors).ErrorMessage);
        userManager.Verify(x => x.DeleteAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LastAdmin_ReturnsForbidden()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        var target = CreateUser("admin-2");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.FindByIdAsync(target.Id)).ReturnsAsync(target);
        userManager
            .Setup(x => x.GetRolesAsync(caller))
            .ReturnsAsync((IList<string>)[UserAccountHardDelete.AdminRoleName]);
        userManager
            .Setup(x => x.GetRolesAsync(target))
            .ReturnsAsync((IList<string>)[UserAccountHardDelete.AdminRoleName]);
        userManager
            .Setup(x => x.GetUsersInRoleAsync(UserAccountHardDelete.AdminRoleName))
            .ReturnsAsync(new List<AppUser> { target });

        var sut = CreateSut(userManager, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(target.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "Cannot delete the last administrator account.",
            Assert.Single(result.Errors).ErrorMessage);
        userManager.Verify(x => x.DeleteAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingTarget_ReturnsNotFound()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager
            .Setup(x => x.GetRolesAsync(caller))
            .ReturnsAsync((IList<string>)[UserAccountHardDelete.AdminRoleName]);
        userManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((AppUser?)null);

        var sut = CreateSut(userManager, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand("missing"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_AdminDeletesUser_Succeeds()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Loose);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var caller = CreateUser("admin-1");
        var target = CreateUser("user-2");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.FindByIdAsync(target.Id)).ReturnsAsync(target);
        userManager
            .Setup(x => x.GetRolesAsync(caller))
            .ReturnsAsync((IList<string>)[UserAccountHardDelete.AdminRoleName]);
        userManager
            .Setup(x => x.GetRolesAsync(target))
            .ReturnsAsync((IList<string>)[]);
        userManager.Setup(x => x.DeleteAsync(target)).ReturnsAsync(IdentityResult.Success);

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

        var sut = CreateSut(userManager, currentUser, commentRepo, blogRepo, imageStorage, unitOfWork);
        var result = await sut.Handle(new DeleteUserCommand(target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        userManager.Verify(x => x.DeleteAsync(target), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static DeleteUserCommandHandler CreateSut(
        Mock<UserManager<AppUser>> userManager,
        Mock<ICurrentUserService> currentUser,
        Mock<IRepository<Comment>> commentRepo,
        Mock<IRepository<Blog>> blogRepo,
        Mock<IImageStorageService> imageStorage,
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            userManager.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            imageStorage.Object,
            unitOfWork.Object);

    private static AppUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = "Test",
        LastName = "User"
    };

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
