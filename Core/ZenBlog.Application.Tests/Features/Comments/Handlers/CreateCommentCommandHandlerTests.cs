using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Comments.Handlers;

public class CreateCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_AlwaysUsesAuthenticatedUserId_InsteadOfCommandUserId()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var commandUserId = "payload-user-id";
        var authenticatedUserId = "jwt-user-id";
        var command = new CreateCommentCommand
        {
            Body = "Security comment body",
            BlogId = Guid.NewGuid(),
            ParentCommentId = Guid.NewGuid(),
            UserId = commandUserId
        };

        var mappedComment = new Comment
        {
            Id = Guid.NewGuid(),
            Body = command.Body,
            BlogId = command.BlogId,
            ParentCommentId = command.ParentCommentId,
            UserId = commandUserId
        };

        Comment? createdEntity = null;

        mapper
            .Setup(x => x.Map<Comment>(command))
            .Returns(mappedComment);
        currentUser
            .SetupGet(x => x.UserId)
            .Returns(authenticatedUserId);
        repository
            .Setup(x => x.CreateAsync(It.IsAny<Comment>()))
            .Callback<Comment>(comment => createdEntity = comment)
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var sut = new CreateCommentCommandHandler(
            repository.Object,
            unitOfWork.Object,
            mapper.Object,
            currentUser.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var data = Assert.IsType<ZenBlog.Application.Features.Comments.Results.CreateCommentResult>(result.Data);
        Assert.Equal(mappedComment.Id, data.Id);
        Assert.Equal(mappedComment.Body, data.Body);
        Assert.Equal(mappedComment.BlogId, data.BlogId);
        Assert.Equal(mappedComment.ParentCommentId, data.ParentCommentId);

        var persisted = Assert.IsType<Comment>(createdEntity);
        Assert.Equal(authenticatedUserId, persisted.UserId);
        Assert.NotEqual(commandUserId, persisted.UserId);
    }
}
