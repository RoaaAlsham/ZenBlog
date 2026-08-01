using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Monitoring.Results;

namespace ZenBlog.Application.Features.Monitoring.Queries;

public sealed record GetActivityLogsQuery(
    int? Page = null,
    int? PageSize = null,
    DateTime? From = null,
    DateTime? To = null,
    string? Action = null,
    string? Search = null) : IRequest<BaseResult<PagedResult<ActivityLogResult>>>;
