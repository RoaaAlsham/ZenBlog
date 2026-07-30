using FluentValidation;
using ZenBlog.Application.Features.Users.Commands;

namespace ZenBlog.Application.Features.Users.Validators;

public class PromoteUserToAdminCommandValidator : AbstractValidator<PromoteUserToAdminCommand>
{
    public PromoteUserToAdminCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
