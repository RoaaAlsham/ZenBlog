using FluentValidation;
using ZenBlog.Application.Features.Monitoring.Queries;

namespace ZenBlog.Application.Features.Monitoring.Validators;

public sealed class GetSecurityRequestLogsQueryValidator : AbstractValidator<GetSecurityRequestLogsQuery>
{
    public GetSecurityRequestLogsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Page.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .When(x => x.PageSize.HasValue);

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("'From' must be less than or equal to 'To'.");

        RuleFor(x => x.EventType)
            .IsInEnum()
            .When(x => x.EventType.HasValue);
    }
}
