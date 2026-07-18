using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class DeleteMyAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailureAndDoesNotDelete()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var user = CreateUser("u1");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(x => x.CheckPasswordAsync(user, "Wrong!")).ReturnsAsync(false);

        var sut = new DeleteMyAccountCommandHandler(
            userManager.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new DeleteMyAccountCommand { CurrentPassword = "Wrong!" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Incorrect password.", Assert.Single(result.Errors).ErrorMessage);
        userManager.Verify(x => x.DeleteAsync(It.IsAny<AppUser>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPassword_PurgesContentAndDeletesUser()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var user = CreateUser("u1");
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Post",
            Description = "Desc",
            CategoryId = Guid.NewGuid(),
            UserId = user.Id
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
        userManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

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
            .ReturnsAsync([blog]);

        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = new DeleteMyAccountCommandHandler(
            userManager.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new DeleteMyAccountCommand { CurrentPassword = "Password123!" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        blogRepo.Verify(x => x.DeleteAsync(blog), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        userManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var userManager = CreateUserManagerMock();
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = new DeleteMyAccountCommandHandler(
            userManager.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new DeleteMyAccountCommand { CurrentPassword = "Password123!" },
            CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    private static AppUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = "zenuser",
        Email = "zen@example.com",
        FirstName = "Zen",
        LastName = "User"
    };

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
