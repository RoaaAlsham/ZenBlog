using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Settings.Commands;
using ZenBlog.Application.Features.Settings.Queries;

namespace ZenBlog.API.Endpoints;

public static class SettingsEndpoints
{
    public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder erb)
    {
        var settings = erb.MapGroup("/settings").WithTags("Settings");

        settings.MapGet("", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetSiteSettingsQuery());
            return result.ToHttpResult();
        });

        settings.MapPut("", async (IMediator mediator, UpdateSiteSettingsCommand command) =>
        {
            var result = await mediator.Send(command);
            return result.ToHttpResult();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
