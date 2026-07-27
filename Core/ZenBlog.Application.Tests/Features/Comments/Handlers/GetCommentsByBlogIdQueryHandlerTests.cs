using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.DTOs.ZenBlog.Application.DTOs;
using ZenBlog.Application.Features.Comments.Handlers;
using ZenBlog.Application.Features.Comments.Queries;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Comments.Handlers;

public class GetCommentsByBlogIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_LoadsRepliesWithUserIncludePaths_AndMapsReplyAuthors()
    {
        var blogId = Guid.NewGuid();
        var repository = new Mock<IRepository<Comment>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        var replyAuthor = new AppUser
        {
            Id = "reply-author-id",
            UserName = "replyer",
            Email = "reply@example.com",
            FirstName = "Reply",
            LastName = "Author"
        };
        var topLevel = new Comment
        {
            Id = Guid.NewGuid(),
            Body = "Parent comment",
            BlogId = blogId,
            UserId = "parent-author",
            User = new AppUser
            {
                Id = "parent-author",
                UserName = "parent",
                Email = "parent@example.com",
                FirstName = "Parent",
                LastName = "Author"
            },
            Replies =
            [
                new Comment
                {
                    Id = Guid.NewGuid(),
                    Body = "Reply body",
                    BlogId = blogId,
                    UserId = replyAuthor.Id,
                    User = replyAuthor
                }
            ]
        };

        string[]? capturedPaths = null;
        repository
            .Setup(x => x.GetPagedWithIncludePathsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Comment, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .Callback<System.Linq.Expressions.Expression<Func<Comment, bool>>, int, int, CancellationToken, string[]>(
                (_, _, _, _, paths) => capturedPaths = paths)
            .ReturnsAsync(([topLevel], 1));

        var mapped = new List<CommentResult>
        {
            new()
            {
                Id = topLevel.Id,
                Body = topLevel.Body,
                BlogId = blogId,
                UserId = topLevel.UserId,
                User = new UserDto { Id = "parent-author", Username = "parent" },
                Replies =
                [
                    new CommentResult
                    {
                        Id = topLevel.Replies[0].Id,
                        Body = "Reply body",
                        BlogId = blogId,
                        UserId = replyAuthor.Id,
                        User = new UserDto { Id = replyAuthor.Id, Username = "replyer" }
                    }
                ]
            }
        };

        mapper
            .Setup(x => x.Map<List<CommentResult>>(It.IsAny<List<Comment>>()))
            .Returns(mapped);

        var sut = new GetCommentsByBlogIdQueryHandler(repository.Object, mapper.Object);
        var result = await sut.Handle(new GetCommentsByBlogIdQuery(blogId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedPaths);
        Assert.Contains("User", capturedPaths!);
        Assert.Contains("Replies", capturedPaths!);
        Assert.Contains("Replies.User", capturedPaths!);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal(1, result.Data.Page);

        var reply = Assert.Single(Assert.Single(result.Data.Items).Replies);
        Assert.NotNull(reply.User);
        Assert.Equal("replyer", reply.User.Username);
    }
}
