using Microsoft.Extensions.Logging;
using Moq;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Media.Commands;
using ZenBlog.Application.Features.Media.Handlers;

namespace ZenBlog.Application.Tests.Features.Media.Handlers;

public class UploadImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        var storage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UploadImageCommandHandler>>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        currentUser.SetupGet(x => x.UserId).Returns((string?)null);

        var sut = new UploadImageCommandHandler(storage.Object, currentUser.Object, logger.Object);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await sut.Handle(
            new UploadImageCommand
            {
                Purpose = ImageUploadPurpose.Profile,
                Content = stream,
                FileName = "a.png",
                ContentType = "image/png",
                Length = 3
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("You are not authenticated.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Handle_Authenticated_UploadsAndReturnsUrlAndPublicId()
    {
        var storage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UploadImageCommandHandler>>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("u1");

        storage
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                "a.png",
                "image/png",
                "zenblog/profiles",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredImage(
                "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/a.png",
                "zenblog/profiles/a"));

        var sut = new UploadImageCommandHandler(storage.Object, currentUser.Object, logger.Object);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await sut.Handle(
            new UploadImageCommand
            {
                Purpose = ImageUploadPurpose.Profile,
                Content = stream,
                FileName = "a.png",
                ContentType = "image/png",
                Length = 3
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("zenblog/profiles/a", result.Data!.PublicId);
        Assert.Contains("res.cloudinary.com", result.Data.Url);
    }

    [Fact]
    public async Task Handle_StorageThrows_ReturnsGenericFailureWithoutExceptionMessage()
    {
        var storage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UploadImageCommandHandler>>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns("u1");

        storage
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret cloudinary detail"));

        var sut = new UploadImageCommandHandler(storage.Object, currentUser.Object, logger.Object);
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await sut.Handle(
            new UploadImageCommand
            {
                Purpose = ImageUploadPurpose.BlogCover,
                Content = stream,
                FileName = "a.png",
                ContentType = "image/png",
                Length = 3
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image upload failed.", Assert.Single(result.Errors).ErrorMessage);
        Assert.DoesNotContain("secret", Assert.Single(result.Errors).ErrorMessage);
    }
}
