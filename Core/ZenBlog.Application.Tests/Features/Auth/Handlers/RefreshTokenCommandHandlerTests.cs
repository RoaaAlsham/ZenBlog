using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Auth.Handlers;

public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidToken_RotatesTokenAndRevokesPreviousToken()
    {
        var userManager = CreateUserManagerMock();
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
        var roles = (IList<string>)new List<string> { "User" };
        var incomingRefreshToken = "incoming-raw-token";
        var incomingRefreshTokenHash = "incoming-hash";
        var newRefreshToken = "new-raw-token";
        var newRefreshTokenHash = "new-hash";
        var newRefreshExpiry = DateTime.UtcNow.AddDays(7);
        var accessToken = "access-token";
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = incomingRefreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = null
        };

        RefreshToken? createdToken = null;

        refreshTokenService
            .Setup(x => x.HashToken(incomingRefreshToken))
            .Returns(incomingRefreshTokenHash);
        refreshTokenRepository
            .Setup(x => x.GetSingleAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        userManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync((IList<string>)roles);
        tokenGenerator
            .Setup(x => x.GenerateToken(user, roles, null))
            .Returns((accessToken, accessExpiry));
        refreshTokenService
            .Setup(x => x.GenerateRefreshToken(7))
            .Returns((newRefreshToken, newRefreshTokenHash, newRefreshExpiry));
        refreshTokenRepository
            .Setup(x => x.UpdateAsync(existingToken))
            .Returns(Task.CompletedTask);
        refreshTokenRepository
            .Setup(x => x.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(rt => createdToken = rt)
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var sut = new RefreshTokenCommandHandler(
            userManager.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = incomingRefreshToken }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(existingToken.RevokedAtUtc);
        Assert.Equal(newRefreshTokenHash, existingToken.ReplacedByTokenHash);

        var created = Assert.IsType<RefreshToken>(createdToken);
        Assert.Equal(user.Id, created.UserId);
        Assert.Equal(newRefreshTokenHash, created.TokenHash);
        Assert.Equal(newRefreshExpiry, created.ExpiresAtUtc);

        var data = Assert.IsType<ZenBlog.Application.Features.Auth.Results.RefreshTokenResult>(result.Data);
        Assert.Equal(accessToken, data.AccessToken);
        Assert.Equal(accessExpiry, data.AccessTokenExpiresAtUtc);
        Assert.Equal(newRefreshToken, data.RefreshToken);
        Assert.Equal(newRefreshExpiry, data.RefreshTokenExpiresAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_NullOrEmptyRefreshToken_ReturnsFailureWithoutHashing(string? refreshToken)
    {
        var userManager = CreateUserManagerMock();
        var tokenGenerator = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var sut = new RefreshTokenCommandHandler(
            userManager.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new RefreshTokenCommand { RefreshToken = refreshToken! },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid refresh token.", Assert.Single(result.Errors).ErrorMessage);
        refreshTokenService.Verify(x => x.HashToken(It.IsAny<string>()), Times.Never);
        refreshTokenRepository.Verify(
            x => x.GetSingleAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesFamilyAndReturnsUnauthorized()
    {
        var userManager = CreateUserManagerMock();
        var tokenGenerator = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var incomingRefreshToken = "incoming-raw-token";
        var incomingRefreshTokenHash = "incoming-hash";
        var userId = "u1";
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = incomingRefreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        var activeSibling = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "sibling-hash",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RevokedAtUtc = null
        };

        refreshTokenService
            .Setup(x => x.HashToken(incomingRefreshToken))
            .Returns(incomingRefreshTokenHash);
        refreshTokenRepository
            .Setup(x => x.GetSingleAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);
        refreshTokenRepository
            .Setup(x => x.GetAllWithIncludesAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, object>>[]>()))
            .ReturnsAsync([activeSibling]);
        refreshTokenRepository
            .Setup(x => x.UpdateAsync(activeSibling))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(true);

        var sut = new RefreshTokenCommandHandler(
            userManager.Object,
            tokenGenerator.Object,
            refreshTokenService.Object,
            refreshTokenRepository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = incomingRefreshToken }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid refresh token.", Assert.Single(result.Errors).ErrorMessage);
        Assert.NotNull(activeSibling.RevokedAtUtc);
        refreshTokenRepository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>()), Times.Never);
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
