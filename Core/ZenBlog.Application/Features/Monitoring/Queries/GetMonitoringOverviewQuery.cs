using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Monitoring.Results;

namespace ZenBlog.Application.Features.Monitoring.Queries;

public sealed record GetMonitoringOverviewQuery
    : IRequest<BaseResult<MonitoringOverviewResult>>;
