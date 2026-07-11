using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Validators;

namespace ZenBlog.Application.Tests.Features.Users.Validators;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void NewPassword_MustMeetStrengthRules()
    {
        var invalid = new ChangePasswordCommand
        {
            CurrentPassword = "Password123!",
            NewPassword = "weak"
        };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.NewPassword);

        var valid = new ChangePasswordCommand
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NewPassword_MustDifferFromCurrent()
    {
        var command = new ChangePasswordCommand
        {
            CurrentPassword = "Password123!",
            NewPassword = "Password123!"
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
