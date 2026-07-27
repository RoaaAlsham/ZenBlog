using System.Linq.Expressions;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
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
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var user = CreateUser("u1");

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userQuery
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.CheckPasswordAsync(user, "Wrong!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new DeleteMyAccountCommandHandler(
            userQuery.Object,
            userAccount.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            imageStorage.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new DeleteMyAccountCommand { CurrentPassword = "Wrong!" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Incorrect password.", Assert.Single(result.Errors).ErrorMessage);
        userAccount.Verify(
            x => x.DeleteAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPassword_PurgesContentAndDeletesUser()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Loose);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var user = CreateUser("u1");
        user.ImagePublicId = "zenblog/profiles/me";
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Post",
            Description = "Desc",
            CategoryId = Guid.NewGuid(),
            UserId = user.Id,
            CoverImagePublicId = "zenblog/covers/post"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(user.Id);
        userQuery
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.CheckPasswordAsync(user, "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userAccount
            .Setup(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()))
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
            .ReturnsAsync([blog]);

        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/profiles/me", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        imageStorage
            .Setup(x => x.DeleteAsync("zenblog/covers/post", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = new DeleteMyAccountCommandHandler(
            userQuery.Object,
            userAccount.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            imageStorage.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new DeleteMyAccountCommand { CurrentPassword = "Password123!" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        blogRepo.Verify(x => x.DeleteAsync(blog), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        userAccount.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        imageStorage.Verify(x => x.DeleteAsync("zenblog/profiles/me", It.IsAny<CancellationToken>()), Times.Once);
        imageStorage.Verify(x => x.DeleteAsync("zenblog/covers/post", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var commentRepo = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepo = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = new DeleteMyAccountCommandHandler(
            userQuery.Object,
            userAccount.Object,
            currentUser.Object,
            commentRepo.Object,
            blogRepo.Object,
            imageStorage.Object,
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
}
