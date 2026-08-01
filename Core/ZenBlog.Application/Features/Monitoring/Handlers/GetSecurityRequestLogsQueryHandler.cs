using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Application.Features.Monitoring.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Monitoring.Handlers;

public sealed class GetSecurityRequestLogsQueryHandler(
    ICurrentUserService currentUser,
    IRoleChecker roleChecker,
    IRepository<SecurityRequestLog> securityRepository)
    : IRequestHandler<GetSecurityRequestLogsQuery, BaseResult<PagedResult<SecurityRequestLogResult>>>
{
    public async Task<BaseResult<PagedResult<SecurityRequestLogResult>>> Handle(
        GetSecurityRequestLogsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<PagedResult<SecurityRequestLogResult>>.Unauthorized(
                "You must be signed in to view security request logs.");
        }

        if (!await roleChecker.IsInRoleAsync(currentUser.UserId, "Admin", cancellationToken))
        {
            return BaseResult<PagedResult<SecurityRequestLogResult>>.Forbidden(
                "Only administrators can view security request logs.");
        }

        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);

        var (items, totalCount) = await securityRepository.GetPagedWithIncludePathsAsync(
            s =>
                (!request.From.HasValue || s.OccurredAtUtc >= request.From.Value)
                && (!request.To.HasValue || s.OccurredAtUtc <= request.To.Value)
                && (!request.EventType.HasValue || s.EventType == request.EventType.Value),
            page,
            pageSize,
            cancellationToken);

        var mapped = items.Select(s => new SecurityRequestLogResult(
            s.Id,
            s.OccurredAtUtc,
            s.SourceIp,
            s.Host,
            s.Path,
            s.EventType,
            s.StatusCode)).ToList();

        return BaseResult<PagedResult<SecurityRequestLogResult>>.Success(
            PagedResult<SecurityRequestLogResult>.Create(mapped, page, pageSize, totalCount));
    }
}
