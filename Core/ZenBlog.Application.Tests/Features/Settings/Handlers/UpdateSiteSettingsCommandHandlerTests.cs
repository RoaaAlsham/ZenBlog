using Microsoft.AspNetCore.Identity;
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
        var userManager = CreateUserManagerMock();
        var caller = new AppUser
        {
            Id = "user-1",
            Email = "user@example.com",
            UserName = "user",
            FirstName = "Regular",
            LastName = "User"
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.GetRolesAsync(caller)).ReturnsAsync((IList<string>)new List<string>());

        var sut = new UpdateSiteSettingsCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            userManager.Object);

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
        var userManager = CreateUserManagerMock();
        var caller = new AppUser
        {
            Id = "admin-1",
            Email = "admin@example.com",
            UserName = "admin",
            FirstName = "Site",
            LastName = "Admin"
        };
        var settings = new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            AllowRegistrations = false
        };

        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager
            .Setup(x => x.GetRolesAsync(caller))
            .ReturnsAsync((IList<string>)new List<string> { "Admin" });
        repository
            .Setup(x => x.GetByIdAsync(SiteSettings.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        repository.Setup(x => x.UpdateAsync(settings)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = new UpdateSiteSettingsCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            userManager.Object);

        var result = await sut.Handle(new UpdateSiteSettingsCommand(true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.AllowRegistrations);
        Assert.True(settings.AllowRegistrations);
        repository.Verify(x => x.UpdateAsync(settings), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
