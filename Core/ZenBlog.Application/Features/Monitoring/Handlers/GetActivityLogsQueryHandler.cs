using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Application.Features.Monitoring.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Monitoring.Handlers;

public sealed class GetActivityLogsQueryHandler(
    ICurrentUserService currentUser,
    IRepository<ActivityLog> activityRepository)
    : IRequestHandler<GetActivityLogsQuery, BaseResult<PagedResult<ActivityLogResult>>>
{
    public async Task<BaseResult<PagedResult<ActivityLogResult>>> Handle(
        GetActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<PagedResult<ActivityLogResult>>.Unauthorized(
                "You must be signed in to view activity logs.");
        }

        // AuthDeep owns roles. The gateway asserted them, its signature was
        // verified before this handler ran, and AuthDeepRoleMap already added the
        // canonical "Admin" alongside aliases like tenant_admin — the very claim
        // RequireRole("Admin") matched on the endpoint. Asking AspNetUserRoles
        // instead was the one check that disagreed, and the only reason every
        // monitoring call came back 403 while the rest of the admin surface
        // worked. ICurrentUserService says that table no longer decides this.
        if (!currentUser.IsAdmin)
        {
            return BaseResult<PagedResult<ActivityLogResult>>.Forbidden(
                "Only administrators can view activity logs.");
        }

        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);
        var actionFilter = string.IsNullOrWhiteSpace(request.Action)
            ? null
            : request.Action.Trim();
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim().ToLowerInvariant();

        var (items, totalCount) = await activityRepository.GetPagedWithIncludePathsAsync(
            a =>
                (!request.From.HasValue || a.OccurredAtUtc >= request.From.Value)
                && (!request.To.HasValue || a.OccurredAtUtc <= request.To.Value)
                && (actionFilter == null || a.Action == actionFilter)
                && (search == null
                    || (a.ActorDisplayName != null && a.ActorDisplayName.ToLower().Contains(search))
                    || a.Summary.ToLower().Contains(search)
                    || a.Action.ToLower().Contains(search)),
            page,
            pageSize,
            cancellationToken);

        var mapped = items.Select(a => new ActivityLogResult(
            a.Id,
            a.OccurredAtUtc,
            a.ActorUserId,
            a.ActorDisplayName,
            a.Action,
            a.EntityType,
            a.EntityId,
            a.Summary,
            a.Success)).ToList();

        return BaseResult<PagedResult<ActivityLogResult>>.Success(
            PagedResult<ActivityLogResult>.Create(mapped, page, pageSize, totalCount));
    }
}
