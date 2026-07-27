using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Settings.Commands;
using ZenBlog.Application.Features.Settings.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Settings.Handlers;

public class UpdateSiteSettingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonAdmin_ReturnsForbidden()
    {
        var repository = new Mock<IRepository<SiteSettings>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var callerId = "user-1";

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(callerId);
        roleChecker
            .Setup(x => x.IsInRoleAsync(callerId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new UpdateSiteSettingsCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            roleChecker.Object);

        var result = await sut.Handle(new UpdateSiteSettingsCommand(true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        repository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Admin_UpdatesAllowRegistrations()
    {
        var repository = new Mock<IRepository<SiteSettings>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var callerId = "admin-1";
        var settings = new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            AllowRegistrations = false
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(callerId);
        roleChecker
            .Setup(x => x.IsInRoleAsync(callerId, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(x => x.GetByIdAsync(SiteSettings.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        repository.Setup(x => x.UpdateAsync(settings)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = new UpdateSiteSettingsCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            roleChecker.Object);

        var result = await sut.Handle(new UpdateSiteSettingsCommand(true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.AllowRegistrations);
        Assert.True(settings.AllowRegistrations);
        repository.Verify(x => x.UpdateAsync(settings), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
