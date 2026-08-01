using FluentValidation;
using ZenBlog.Application.Features.Monitoring.Queries;

namespace ZenBlog.Application.Features.Monitoring.Validators;

public sealed class GetMonitoringOverviewQueryValidator : AbstractValidator<GetMonitoringOverviewQuery>
{
    public GetMonitoringOverviewQueryValidator()
    {
    }
}
