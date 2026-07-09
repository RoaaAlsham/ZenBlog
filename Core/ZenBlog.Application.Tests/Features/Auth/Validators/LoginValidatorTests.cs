using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Validators;

namespace ZenBlog.Application.Tests.Features.Auth.Validators;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void EmailNotEmptyRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = new LoginCommand { Email = string.Empty, Password = "Password123!" };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Email);

        var valid = new LoginCommand { Email = "user@example.com", Password = "Password123!" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailFormatRule_FailsWhenInvalid_PassesWhenValid()
    {
        var invalid = new LoginCommand { Email = "not-an-email", Password = "Password123!" };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Email);

        var valid = new LoginCommand { Email = "valid@example.com", Password = "Password123!" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void PasswordNotEmptyRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = new LoginCommand { Email = "user@example.com", Password = string.Empty };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Password);

        var valid = new LoginCommand { Email = "user@example.com", Password = "Password123!" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
