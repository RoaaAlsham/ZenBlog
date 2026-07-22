using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Auth.Handlers;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_KnownActiveToken_RevokesAndReturnsSuccess()
    {
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var repository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var raw = "raw-refresh";
        var hash = "token-hash";
        var existing = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            TokenHash = hash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RevokedAtUtc = null
        };

        refreshTokenService.Setup(x => x.HashToken(raw)).Returns(hash);
        repository
            .Setup(x => x.GetSingleAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository.Setup(x => x.UpdateAsync(existing)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var sut = new LogoutCommandHandler(
            refreshTokenService.Object,
            repository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(new LogoutCommand { RefreshToken = raw }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.NotNull(existing.RevokedAtUtc);
        repository.Verify(x => x.UpdateAsync(existing), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyToken_ReturnsSuccessWithoutLookup(string? refreshToken)
    {
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var repository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var sut = new LogoutCommandHandler(
            refreshTokenService.Object,
            repository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(new LogoutCommand { RefreshToken = refreshToken }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        refreshTokenService.Verify(x => x.HashToken(It.IsAny<string>()), Times.Never);
        repository.Verify(
            x => x.GetSingleAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsSuccessWithoutUpdate()
    {
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var repository = new Mock<IRepository<RefreshToken>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        refreshTokenService.Setup(x => x.HashToken("unknown")).Returns("hash");
        repository
            .Setup(x => x.GetSingleAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var sut = new LogoutCommandHandler(
            refreshTokenService.Object,
            repository.Object,
            unitOfWork.Object);

        var result = await sut.Handle(new LogoutCommand { RefreshToken = "unknown" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(x => x.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
