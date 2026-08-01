using FluentValidation;
using ZenBlog.Application.Features.Monitoring.Queries;

namespace ZenBlog.Application.Features.Monitoring.Validators;

public sealed class GetActivityLogsQueryValidator : AbstractValidator<GetActivityLogsQuery>
{
    public GetActivityLogsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Page.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .When(x => x.PageSize.HasValue);

        RuleFor(x => x.Action)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Action));

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("'From' must be less than or equal to 'To'.");
    }
}
