using AutoMapper;
using Moq;
using ZenBlog.Application.Base;
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
        var blogRepository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
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

        blogRepository
            .Setup(x => x.GetByIdAsync(command.BlogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Blog
            {
                Id = command.BlogId,
                Title = "T",
                Description = "D",
                CategoryId = Guid.NewGuid(),
                UserId = "author"
            });
        repository
            .Setup(x => x.GetByIdAsync(command.ParentCommentId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Comment
            {
                Id = command.ParentCommentId!.Value,
                Body = "Parent",
                BlogId = command.BlogId,
                UserId = "parent-author"
            });
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
            blogRepository.Object,
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

    [Fact]
    public async Task Handle_MissingBlog_ReturnsNotFound()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var command = new CreateCommentCommand
        {
            Body = "Orphan comment",
            BlogId = Guid.NewGuid(),
            UserId = "ignored"
        };

        blogRepository
            .Setup(x => x.GetByIdAsync(command.BlogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Blog?)null);

        var sut = new CreateCommentCommandHandler(
            repository.Object,
            blogRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            currentUser.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        repository.Verify(x => x.CreateAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ParentOnDifferentBlog_ReturnsFailure()
    {
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var blogRepository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var blogId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var command = new CreateCommentCommand
        {
            Body = "Cross-blog reply",
            BlogId = blogId,
            ParentCommentId = parentId,
            UserId = "ignored"
        };

        blogRepository
            .Setup(x => x.GetByIdAsync(blogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Blog
            {
                Id = blogId,
                Title = "T",
                Description = "D",
                CategoryId = Guid.NewGuid(),
                UserId = "author"
            });
        repository
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Comment
            {
                Id = parentId,
                Body = "Parent",
                BlogId = Guid.NewGuid(),
                UserId = "parent-author"
            });

        var sut = new CreateCommentCommandHandler(
            repository.Object,
            blogRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            currentUser.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Parent comment does not belong to this blog.", Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.CreateAsync(It.IsAny<Comment>()), Times.Never);
    }
}
