using FluentValidation;
using ZenBlog.Application.Features.Auth.Commands;

namespace ZenBlog.Application.Features.Auth.Validators;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Invalid refresh token.");
    }
}
