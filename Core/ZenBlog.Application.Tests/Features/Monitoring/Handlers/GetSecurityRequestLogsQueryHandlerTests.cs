using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring.Handlers;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Monitoring.Handlers;

public class GetSecurityRequestLogsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("user-1");
        currentUser.SetupGet(x => x.Roles)
            .Returns(new[] { "tenant_member" });

        var sut = new GetSecurityRequestLogsQueryHandler(
            currentUser.Object,
            new Mock<IRepository<SecurityRequestLog>>(MockBehavior.Strict).Object);

        var result = await sut.Handle(new GetSecurityRequestLogsQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_Admin_ReturnsPagedSecurityRequests()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var repository = new Mock<IRepository<SecurityRequestLog>>(MockBehavior.Strict);
        var occurred = DateTime.UtcNow;
        var log = new SecurityRequestLog
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurred,
            SourceIp = "127.0.0.1",
            Host = "localhost:7117",
            Path = "/api/auth/login",
            EventType = SecurityEventType.LoginFailure,
            StatusCode = 401,
            CreatedAt = occurred,
            UpdatedAt = occurred
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("admin-1");
        currentUser.SetupGet(x => x.Roles)
            .Returns(new[] { "tenant_admin", "Admin" });
        repository
            .Setup(x => x.GetPagedWithIncludePathsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SecurityRequestLog, bool>>>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SecurityRequestLog> { log }, 1));

        var sut = new GetSecurityRequestLogsQueryHandler(
            currentUser.Object,
            repository.Object);

        var result = await sut.Handle(new GetSecurityRequestLogsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("127.0.0.1", item.SourceIp);
        Assert.Equal("localhost:7117", item.Host);
        Assert.Equal("/api/auth/login", item.Path);
        Assert.Equal(SecurityEventType.LoginFailure, item.EventType);
    }
}
