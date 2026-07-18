using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Validators;

namespace ZenBlog.Application.Tests.Features.Users.Validators;

public class DeleteMyAccountCommandValidatorTests
{
    private readonly DeleteMyAccountCommandValidator _validator = new();

    [Fact]
    public void CurrentPassword_IsRequired()
    {
        var result = _validator.TestValidate(new DeleteMyAccountCommand { CurrentPassword = "" });
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void CurrentPassword_WhenPresent_IsValid()
    {
        var result = _validator.TestValidate(
            new DeleteMyAccountCommand { CurrentPassword = "Password123!" });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
