using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring.Handlers;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Monitoring.Handlers;

public class GetActivityLogsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("user-1");
        currentUser.SetupGet(x => x.Roles)
            .Returns(new[] { "tenant_member" });

        var sut = new GetActivityLogsQueryHandler(
            currentUser.Object,
            new Mock<IRepository<ActivityLog>>(MockBehavior.Strict).Object);

        var result = await sut.Handle(new GetActivityLogsQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_Admin_ReturnsPagedActivity()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var repository = new Mock<IRepository<ActivityLog>>(MockBehavior.Strict);
        var occurred = DateTime.UtcNow;
        var log = new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurred,
            ActorUserId = "u1",
            ActorDisplayName = "Ada Lovelace",
            Action = "Blog.Created",
            EntityType = "Blog",
            EntityId = Guid.NewGuid().ToString(),
            Summary = "Created blog 'Hello'",
            Success = true,
            CreatedAt = occurred,
            UpdatedAt = occurred
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("admin-1");
        currentUser.SetupGet(x => x.Roles)
            .Returns(new[] { "tenant_admin", "Admin" });
        repository
            .Setup(x => x.GetPagedWithIncludePathsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ActivityLog, bool>>>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ActivityLog> { log }, 1));

        var sut = new GetActivityLogsQueryHandler(
            currentUser.Object,
            repository.Object);

        var result = await sut.Handle(new GetActivityLogsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(log.Action, item.Action);
        Assert.Equal(log.Summary, item.Summary);
        Assert.Equal(log.ActorDisplayName, item.ActorDisplayName);
    }
}
