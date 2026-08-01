using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Monitoring.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Monitoring.Queries;

public sealed record GetSecurityRequestLogsQuery(
    int? Page = null,
    int? PageSize = null,
    DateTime? From = null,
    DateTime? To = null,
    SecurityEventType? EventType = null) : IRequest<BaseResult<PagedResult<SecurityRequestLogResult>>>;
