using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Media.Commands;
using ZenBlog.Application.Features.Media.Validators;

namespace ZenBlog.Application.Tests.Features.Media.Validators;

public class UploadImageCommandValidatorTests
{
    private readonly UploadImageCommandValidator _validator = new();

    [Fact]
    public void RejectsDisallowedContentType()
    {
        using var stream = new MemoryStream(new byte[] { 1 });
        var result = _validator.TestValidate(new UploadImageCommand
        {
            Purpose = ImageUploadPurpose.Profile,
            Content = stream,
            FileName = "a.pdf",
            ContentType = "application/pdf",
            Length = 1
        });
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void RejectsOversizedFile()
    {
        using var stream = new MemoryStream(new byte[] { 1 });
        var result = _validator.TestValidate(new UploadImageCommand
        {
            Purpose = ImageUploadPurpose.BlogCover,
            Content = stream,
            FileName = "big.png",
            ContentType = "image/png",
            Length = ImageUploadLimits.MaxBytes + 1
        });
        result.ShouldHaveValidationErrorFor(x => x.Length);
    }

    [Fact]
    public void AcceptsValidImageUnderLimit()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = _validator.TestValidate(new UploadImageCommand
        {
            Purpose = ImageUploadPurpose.BlogBody,
            Content = stream,
            FileName = "a.webp",
            ContentType = "image/webp",
            Length = 3
        });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
