using Moq;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Handlers;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Tests.Helpers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Auth.Handlers;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_UnknownEmail_ReturnsFailureWithGenericMessage()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var tokenGenerator = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var command = new LoginCommand { Email = "missing@example.com", Password = "Password123!" };

        userQuery
            .Setup(x => x.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var sut = new LoginCommandHandler(
            userQuery.Object,
            userAccount.Object,
            roleChecker.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object,
            MonitoringMocks.SecurityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", Assert.Single(result.Errors).ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailureWithSameGenericMessage()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var tokenGenerator = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u1",
            Email = "user@example.com",
            UserName = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var command = new LoginCommand { Email = user.Email!, Password = "WrongPassword!" };

        userQuery
            .Setup(x => x.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.CheckPasswordAsync(user, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new LoginCommandHandler(
            userQuery.Object,
            userAccount.Object,
            roleChecker.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object,
            MonitoringMocks.SecurityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", Assert.Single(result.Errors).ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithTokenGeneratorOutput()
    {
        var userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        var userAccount = new Mock<IUserAccountService>(MockBehavior.Strict);
        var roleChecker = new Mock<IRoleChecker>(MockBehavior.Strict);
        var tokenGenerator = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var user = new AppUser
        {
            Id = "u2",
            Email = "valid@example.com",
            UserName = "valid@example.com",
            FirstName = "Valid",
            LastName = "User"
        };
        var command = new LoginCommand { Email = user.Email!, Password = "CorrectPassword123!" };
        var roles = (IList<string>)new List<string> { "User" };
        var expectedToken = "mock-jwt-token";
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        var expectedRefreshToken = "mock-refresh-token";
        var expectedRefreshTokenHash = "mock-refresh-token-hash";
        var expectedRefreshExpiry = DateTime.UtcNow.AddDays(7);

        userQuery
            .Setup(x => x.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userAccount
            .Setup(x => x.CheckPasswordAsync(user, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        roleChecker
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)roles);
        tokenGenerator
            .Setup(x => x.GenerateToken(user, roles, null))
            .Returns((expectedToken, expectedExpiry));
        refreshTokenService
            .Setup(x => x.GenerateRefreshToken(7))
            .Returns((expectedRefreshToken, expectedRefreshTokenHash, expectedRefreshExpiry));
        refreshTokenRepository
            .Setup(x => x.CreateAsync(It.Is<RefreshToken>(rt =>
                rt.TokenHash == expectedRefreshTokenHash &&
                rt.UserId == user.Id &&
                rt.ExpiresAtUtc == expectedRefreshExpiry)))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var sut = new LoginCommandHandler(
            userQuery.Object,
            userAccount.Object,
            roleChecker.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object,
            MonitoringMocks.SecurityLogger().Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        var data = Assert.IsType<ZenBlog.Application.Features.Auth.Results.LoginResult>(result.Data);
        Assert.Equal(user.Id, data.UserId);
        Assert.Equal(user.Email, data.Email);
        Assert.Equal(user.UserName, data.Username);
        Assert.Equal(user.FirstName, data.FirstName);
        Assert.Equal(user.LastName, data.LastName);
        Assert.Equal(user.ImageUrl, data.ImageUrl);
        Assert.Equal(expectedToken, data.Token);
        Assert.Equal(expectedExpiry, data.ExpiresAtUtc);
        Assert.Equal(expectedRefreshToken, data.RefreshToken);
        Assert.Equal(expectedRefreshExpiry, data.RefreshTokenExpiresAtUtc);
        Assert.Empty(result.Errors);
    }
}
