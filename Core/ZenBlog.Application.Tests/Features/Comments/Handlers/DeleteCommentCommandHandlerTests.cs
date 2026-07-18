using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Comments.Handlers;

public class DeleteCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsForbiddenAndDoesNotDelete()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new RemoveCommentCommand(Guid.NewGuid());
        var comment = CreateComment(command.Id, "comment-owner-id");
        var caller = CreateUser("different-user-id");

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.GetRolesAsync(caller)).ReturnsAsync((IList<string>)["User"]);

        var sut = CreateSut(repository, unitOfWork, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "You are not authorized to delete this comment.",
            Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.DeleteAsync(It.IsAny<Comment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_DeletesSuccessfully()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var ownerId = "comment-owner-id";
        var command = new RemoveCommentCommand(Guid.NewGuid());
        var comment = CreateComment(command.Id, ownerId);

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        repository
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<Expression<Func<Comment, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Comment, object>>[]>()))
            .ReturnsAsync([]);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = CreateSut(repository, unitOfWork, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        repository.Verify(x => x.DeleteAsync(comment), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        userManager.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminNonOwner_DeletesSuccessfully()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Loose);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new RemoveCommentCommand(Guid.NewGuid());
        var comment = CreateComment(command.Id, "comment-owner-id");
        var admin = CreateUser("admin-id");

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        repository
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<Expression<Func<Comment, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Comment, object>>[]>()))
            .ReturnsAsync([]);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(admin.Id);
        userManager.Setup(x => x.FindByIdAsync(admin.Id)).ReturnsAsync(admin);
        userManager.Setup(x => x.GetRolesAsync(admin)).ReturnsAsync((IList<string>)["Admin"]);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = CreateSut(repository, unitOfWork, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(x => x.DeleteAsync(comment), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingComment_ReturnsNotFound()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new RemoveCommentCommand(Guid.NewGuid());
        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        var sut = CreateSut(repository, unitOfWork, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new RemoveCommentCommand(Guid.NewGuid());
        var comment = CreateComment(command.Id, "comment-owner-id");

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = CreateSut(repository, unitOfWork, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        repository.Verify(x => x.DeleteAsync(It.IsAny<Comment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static DeleteCommentCommandHandler CreateSut(
        Mock<IRepository<Comment>> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUserService> currentUser,
        Mock<UserManager<AppUser>> userManager) =>
        new(repository.Object, unitOfWork.Object, currentUser.Object, userManager.Object);

    private static Comment CreateComment(Guid id, string userId) => new()
    {
        Id = id,
        Body = "Hello",
        BlogId = Guid.NewGuid(),
        UserId = userId
    };

    private static AppUser CreateUser(string id) => new()
    {
        Id = id,
        Email = $"{id}@example.com",
        UserName = $"{id}@example.com",
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
