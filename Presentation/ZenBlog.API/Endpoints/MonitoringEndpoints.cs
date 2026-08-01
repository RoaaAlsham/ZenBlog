using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Monitoring.Queries;
using ZenBlog.Domain.Entities;

namespace ZenBlog.API.Endpoints;

public static class MonitoringEndpoints
{
    public static void RegisterMonitoringEndpoints(this IEndpointRouteBuilder erb)
    {
        var monitoring = erb.MapGroup("/monitoring").WithTags("Monitoring");

        monitoring.MapGet("/overview", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetMonitoringOverviewQuery());
            return result.ToHttpResult();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        monitoring.MapGet("/activities", async (
            IMediator mediator,
            int? page,
            int? pageSize,
            DateTime? from,
            DateTime? to,
            string? action,
            string? search) =>
        {
            var result = await mediator.Send(
                new GetActivityLogsQuery(page, pageSize, from, to, action, search));
            return result.ToHttpResult();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        monitoring.MapGet("/security-requests", async (
            IMediator mediator,
            int? page,
            int? pageSize,
            DateTime? from,
            DateTime? to,
            SecurityEventType? eventType) =>
        {
            var result = await mediator.Send(
                new GetSecurityRequestLogsQuery(page, pageSize, from, to, eventType));
            return result.ToHttpResult();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
