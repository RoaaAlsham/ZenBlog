using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Validators;

namespace ZenBlog.Application.Tests.Features.Auth.Validators;

public class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _validator = new();

    [Fact]
    public void RefreshTokenNotEmptyRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = new RefreshTokenCommand { RefreshToken = string.Empty };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Invalid refresh token.");

        var valid = new RefreshTokenCommand { RefreshToken = "valid-refresh-token" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }
}
