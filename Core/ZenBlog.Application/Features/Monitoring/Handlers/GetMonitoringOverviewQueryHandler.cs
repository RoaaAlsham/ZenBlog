using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Application.Features.Monitoring.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Monitoring.Handlers;

public sealed class GetMonitoringOverviewQueryHandler(
    ICurrentUserService currentUser,
    IRepository<ActivityLog> activityRepository,
    IRepository<SecurityRequestLog> securityRepository,
    IRepository<Blog> blogRepository,
    IRepository<Comment> commentRepository)
    : IRequestHandler<GetMonitoringOverviewQuery, BaseResult<MonitoringOverviewResult>>
{
    public async Task<BaseResult<MonitoringOverviewResult>> Handle(
        GetMonitoringOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<MonitoringOverviewResult>.Unauthorized(
                "You must be signed in to view monitoring data.");
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
            return BaseResult<MonitoringOverviewResult>.Forbidden(
                "Only administrators can view monitoring data.");
        }

        var now = DateTime.UtcNow;
        var twentyFourHoursAgo = now.AddHours(-24);
        var sevenDaysAgo = now - MonitoringRetention.MaxAge;
        var thirtyDaysAgo = now.AddDays(-30);

        var newUsersLast24Hours = await activityRepository.CountAsync(
            a => a.Action == ActivityActions.AuthRegistered && a.OccurredAtUtc >= twentyFourHoursAgo,
            cancellationToken);
        var newUsersLast7Days = await activityRepository.CountAsync(
            a => a.Action == ActivityActions.AuthRegistered && a.OccurredAtUtc >= sevenDaysAgo,
            cancellationToken);

        var newBlogsLast24Hours = await blogRepository.CountAsync(
            b => b.CreatedAt >= twentyFourHoursAgo,
            cancellationToken);
        var newBlogsLast30Days = await blogRepository.CountAsync(
            b => b.CreatedAt >= thirtyDaysAgo,
            cancellationToken);

        var newCommentsLast24Hours = await commentRepository.CountAsync(
            c => c.CreatedAt >= twentyFourHoursAgo,
            cancellationToken);
        var newCommentsLast30Days = await commentRepository.CountAsync(
            c => c.CreatedAt >= thirtyDaysAgo,
            cancellationToken);

        var failedLoginsLast24Hours = await securityRepository.CountAsync(
            s => s.EventType == SecurityEventType.LoginFailure && s.OccurredAtUtc >= twentyFourHoursAgo,
            cancellationToken);
        var rateLimitHitsLast24Hours = await securityRepository.CountAsync(
            s => s.EventType == SecurityEventType.RateLimited && s.OccurredAtUtc >= twentyFourHoursAgo,
            cancellationToken);

        return BaseResult<MonitoringOverviewResult>.Success(
            new MonitoringOverviewResult(
                newUsersLast24Hours,
                newUsersLast7Days,
                newBlogsLast24Hours,
                newBlogsLast30Days,
                newCommentsLast24Hours,
                newCommentsLast30Days,
                failedLoginsLast24Hours,
                rateLimitHitsLast24Hours));
    }
}
