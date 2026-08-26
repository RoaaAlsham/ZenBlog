using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Application.Features.Monitoring.Handlers;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Monitoring.Handlers;

public class GetMonitoringOverviewQueryHandlerTests
{
    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = CreateSut(currentUser);
        var result = await sut.Handle(new GetMonitoringOverviewQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("user-1");
        currentUser.SetupGet(x => x.IsAdmin).Returns(false);

        var sut = CreateSut(currentUser);
        var result = await sut.Handle(new GetMonitoringOverviewQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_Admin_ReturnsAggregatedCounts()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var activityRepository = new Mock<IRepository<ActivityLog>>(MockBehavior.Strict);
        var securityRepository = new Mock<IRepository<SecurityRequestLog>>(MockBehavior.Strict);
        var blogRepository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var commentRepository = new Mock<IRepository<Comment>>(MockBehavior.Strict);

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("admin-1");
        currentUser.SetupGet(x => x.IsAdmin).Returns(true);

        activityRepository
            .Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ActivityLog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        securityRepository
            .Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SecurityRequestLog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        blogRepository
            .Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Blog, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        commentRepository
            .Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Comment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var sut = new GetMonitoringOverviewQueryHandler(
            currentUser.Object,
            activityRepository.Object,
            securityRepository.Object,
            blogRepository.Object,
            commentRepository.Object);

        var result = await sut.Handle(new GetMonitoringOverviewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.NewUsersLast24Hours);
        Assert.Equal(2, result.Data.NewUsersLast7Days);
        Assert.Equal(4, result.Data.NewBlogsLast24Hours);
        Assert.Equal(4, result.Data.NewBlogsLast30Days);
        Assert.Equal(5, result.Data.NewCommentsLast24Hours);
        Assert.Equal(5, result.Data.NewCommentsLast30Days);
        Assert.Equal(3, result.Data.FailedLoginsLast24Hours);
        Assert.Equal(3, result.Data.RateLimitHitsLast24Hours);
    }

    private static GetMonitoringOverviewQueryHandler CreateSut(
        Mock<ICurrentUserService> currentUser) =>
        new(
            currentUser.Object,
            new Mock<IRepository<ActivityLog>>(MockBehavior.Strict).Object,
            new Mock<IRepository<SecurityRequestLog>>(MockBehavior.Strict).Object,
            new Mock<IRepository<Blog>>(MockBehavior.Strict).Object,
            new Mock<IRepository<Comment>>(MockBehavior.Strict).Object);
}
