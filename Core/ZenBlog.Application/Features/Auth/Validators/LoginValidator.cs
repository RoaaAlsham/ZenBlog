using FluentValidation;
using ZenBlog.Application.Features.Auth.Commands;

namespace ZenBlog.Application.Features.Auth.Validators
{
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
