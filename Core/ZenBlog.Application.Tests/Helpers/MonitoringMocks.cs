using Moq;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Helpers;

public static class MonitoringMocks
{
    public static Mock<IActivityLogger> ActivityLogger()
    {
        var mock = new Mock<IActivityLogger>(MockBehavior.Loose);
        mock.Setup(x => x.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    public static Mock<ISecurityRequestLogger> SecurityLogger()
    {
        var mock = new Mock<ISecurityRequestLogger>(MockBehavior.Loose);
        mock.Setup(x => x.LogAsync(
                It.IsAny<SecurityEventType>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }
}
