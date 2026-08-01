using AutoMapper;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Handlers;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Users.Handlers;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRegistrationsDisabled_ReturnsFailureAndDoesNotCreateUser()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var settingsRepository = new Mock<IRepository<SiteSettings>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        settingsRepository
            .Setup(x => x.GetByIdAsync(SiteSettings.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings
            {
                Id = SiteSettings.SingletonId,
                AllowRegistrations = false
            });

        var sut = new CreateUserCommandHandler(
            userQuery.Object,
            userAccount.Object,
            mapper.Object,
            settingsRepository.Object,
            unitOfWork.Object,
            MonitoringMocks.ActivityLogger().Object);

        var result = await sut.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Registration is currently disabled.", Assert.Single(result.Errors).ErrorMessage);
        userQuery.Verify(
            x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        userAccount.Verify(
            x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegistrationsEnabled_CreatesUser()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var settingsRepository = new Mock<IRepository<SiteSettings>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var command = ValidCommand();
        var mappedUser = new AppUser
        {
            UserName = command.Username,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        settingsRepository
            .Setup(x => x.GetByIdAsync(SiteSettings.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings
            {
                Id = SiteSettings.SingletonId,
                AllowRegistrations = true
            });
        userQuery
            .Setup(x => x.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        mapper.Setup(x => x.Map<AppUser>(command)).Returns(mappedUser);
        userAccount
            .Setup(x => x.CreateAsync(mappedUser, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var sut = new CreateUserCommandHandler(
            userQuery.Object,
            userAccount.Object,
            mapper.Object,
            settingsRepository.Object,
            unitOfWork.Object,
            MonitoringMocks.ActivityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(command.Email, result.Data!.Email);
        Assert.Equal(command.Username, result.Data.Username);
        Assert.False(string.IsNullOrWhiteSpace(mappedUser.Id));
    }

    private static CreateUserCommand ValidCommand() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Username = "janedoe",
        Email = "jane@example.com",
        Password = "Password1!"
    };
}
