using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Validators;
using ZenBlog.Application.Models;

namespace ZenBlog.Application.Tests.Features.Users.Validators;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new(
        Options.Create(new CloudinarySettings { CloudName = "demo" }));

    [Fact]
    public void Names_RequiredAndMaxLength()
    {
        var invalid = new UpdateProfileCommand
        {
            FirstName = "",
            LastName = new string('x', 51)
        };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.FirstName);
        invalidResult.ShouldHaveValidationErrorFor(x => x.LastName);

        var valid = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User"
        };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ImageUrl_WhenProvided_MustBeCloudinaryDeliveryUrl()
    {
        var invalid = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://cdn.example.com/a.png",
            ImagePublicId = "a"
        };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.ImageUrl);

        var valid = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/a.png",
            ImagePublicId = "zenblog/profiles/a"
        };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void ImageUrlAndPublicId_MustBePaired()
    {
        var unpaired = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/a.png",
            ImagePublicId = null
        };
        var result = _validator.TestValidate(unpaired);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void ImagePublicId_RejectsMismatchWithUrl()
    {
        var command = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/profiles/mine.png",
            ImagePublicId = "zenblog/profiles/victim"
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ImagePublicId);
    }

    [Fact]
    public void ImagePublicId_RejectsPublicIdOutsideProfilesFolder()
    {
        var command = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/covers/c.png",
            ImagePublicId = "zenblog/covers/c"
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ImagePublicId);
    }
}
