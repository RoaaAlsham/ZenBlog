using System.Linq.Expressions;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Handlers;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Comments.Handlers;

public class UpdateCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsForbiddenAndDoesNotUpdate()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new UpdateCommentCommand { Id = Guid.NewGuid(), Body = "Hacked body" };
        var comment = CreateComment(command.Id, "comment-owner-id");
        var caller = CreateUser("different-user-id");

        SetupGetWithIncludes(repository, command.Id, comment);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.GetRolesAsync(caller)).ReturnsAsync((IList<string>)["User"]);

        var sut = CreateSut(repository, unitOfWork, mapper, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(
            "You are not authorized to update this comment.",
            Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.UpdateAsync(It.IsAny<Comment>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_UpdatesSuccessfully()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var ownerId = "comment-owner-id";
        var command = new UpdateCommentCommand { Id = Guid.NewGuid(), Body = "Updated body" };
        var comment = CreateComment(command.Id, ownerId);
        var mapped = new CommentResult
        {
            Body = command.Body,
            BlogId = comment.BlogId,
            UserId = ownerId
        };

        SetupGetWithIncludes(repository, command.Id, comment);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(ownerId);
        mapper.Setup(x => x.Map(command, comment)).Returns(comment);
        repository.Setup(x => x.UpdateAsync(comment)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        mapper.Setup(x => x.Map<CommentResult>(comment)).Returns(mapped);

        var sut = CreateSut(repository, unitOfWork, mapper, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(mapped, result.Data);
        repository.Verify(x => x.UpdateAsync(comment), Times.Once);
        userManager.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminNonOwner_UpdatesSuccessfully()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new UpdateCommentCommand { Id = Guid.NewGuid(), Body = "Admin edit" };
        var comment = CreateComment(command.Id, "comment-owner-id");
        var admin = CreateUser("admin-id");
        var mapped = new CommentResult
        {
            Body = command.Body,
            BlogId = comment.BlogId,
            UserId = comment.UserId
        };

        SetupGetWithIncludes(repository, command.Id, comment);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(admin.Id);
        userManager.Setup(x => x.FindByIdAsync(admin.Id)).ReturnsAsync(admin);
        userManager.Setup(x => x.GetRolesAsync(admin)).ReturnsAsync((IList<string>)["Admin"]);
        mapper.Setup(x => x.Map(command, comment)).Returns(comment);
        repository.Setup(x => x.UpdateAsync(comment)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        mapper.Setup(x => x.Map<CommentResult>(comment)).Returns(mapped);

        var sut = CreateSut(repository, unitOfWork, mapper, currentUser, userManager);
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(x => x.UpdateAsync(comment), Times.Once);
    }

    private static void SetupGetWithIncludes(
        Mock<IRepository<Comment>> repository,
        Guid id,
        Comment comment)
    {
        repository
            .Setup(x => x.GetSingleWithIncludesAsync(
                It.IsAny<Expression<Func<Comment, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Comment, object>>[]>()))
            .ReturnsAsync(comment);
    }

    private static UpdateCommentCommandHandler CreateSut(
        Mock<IRepository<Comment>> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IMapper> mapper,
        Mock<ICurrentUserService> currentUser,
        Mock<UserManager<AppUser>> userManager) =>
        new(
            repository.Object,
            unitOfWork.Object,
            mapper.Object,
            currentUser.Object,
            userManager.Object);

    private static Comment CreateComment(Guid id, string userId) => new()
    {
        Id = id,
        Body = "Original",
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
