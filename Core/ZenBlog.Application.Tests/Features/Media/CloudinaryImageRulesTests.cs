using ZenBlog.Application.Features.Media;

namespace ZenBlog.Application.Tests.Features.Media;

public class CloudinaryImageRulesTests
{
    private const string CloudName = "demo";

    [Theory]
    [InlineData(
        "https://res.cloudinary.com/demo/image/upload/v1/zenblog/covers/c.png",
        "zenblog/covers/c")]
    [InlineData(
        "https://res.cloudinary.com/demo/image/upload/zenblog/covers/c.png",
        "zenblog/covers/c")]
    [InlineData(
        "https://res.cloudinary.com/demo/image/upload/w_400,c_fill/v1700000000/zenblog/covers/photo.jpg",
        "zenblog/covers/photo")]
    [InlineData(
        "https://res.cloudinary.com/demo/image/upload/v12/zenblog/profiles/a.webp",
        "zenblog/profiles/a")]
    public void TryExtractPublicIdFromDeliveryUrl_ExtractsExpectedPublicId(
        string url,
        string expectedPublicId)
    {
        var ok = CloudinaryImageRules.TryExtractPublicIdFromDeliveryUrl(
            url, CloudName, out var publicId);

        Assert.True(ok);
        Assert.Equal(expectedPublicId, publicId);
    }

    [Fact]
    public void TryExtractPublicIdFromDeliveryUrl_RejectsWrongCloud()
    {
        var ok = CloudinaryImageRules.TryExtractPublicIdFromDeliveryUrl(
            "https://res.cloudinary.com/other/image/upload/v1/zenblog/covers/c.png",
            CloudName,
            out var publicId);

        Assert.False(ok);
        Assert.Null(publicId);
    }

    [Fact]
    public void PublicIdMatchesDeliveryUrl_RejectsMismatch()
    {
        var url = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/covers/mine.png";

        Assert.False(CloudinaryImageRules.PublicIdMatchesDeliveryUrl(
            url, "zenblog/covers/victim", CloudName));
        Assert.True(CloudinaryImageRules.PublicIdMatchesDeliveryUrl(
            url, "zenblog/covers/mine", CloudName));
    }

    [Theory]
    [InlineData("zenblog/covers", "zenblog/covers", true)]
    [InlineData("zenblog/covers/c", "zenblog/covers", true)]
    [InlineData("zenblog/profiles/a", "zenblog/covers", false)]
    [InlineData("zenblog/covers-extra/x", "zenblog/covers", false)]
    [InlineData(null, "zenblog/covers", false)]
    public void HasFolderPrefix_ChecksExactOrChildPath(
        string? publicId,
        string folder,
        bool expected)
    {
        Assert.Equal(expected, CloudinaryImageRules.HasFolderPrefix(publicId, folder));
    }
}
