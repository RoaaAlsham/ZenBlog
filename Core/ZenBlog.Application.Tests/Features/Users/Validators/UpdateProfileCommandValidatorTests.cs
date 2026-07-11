using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Validators;

namespace ZenBlog.Application.Tests.Features.Users.Validators;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

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
    public void ImageUrl_WhenProvided_MustBeAbsoluteHttpOrHttps()
    {
        var invalid = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "not-a-url"
        };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.ImageUrl);

        var valid = new UpdateProfileCommand
        {
            FirstName = "Zen",
            LastName = "User",
            ImageUrl = "https://cdn.example.com/a.png"
        };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }
}
