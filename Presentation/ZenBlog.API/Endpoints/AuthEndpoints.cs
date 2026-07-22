using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Auth.Commands;

namespace ZenBlog.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void RegisterAuthEndpoints(this IEndpointRouteBuilder erb)
        {
            var auth = erb.MapGroup("/auth").WithTags("Auth");

            auth.MapPost("/login", async (IMediator mediator, LoginCommand command) =>
            {
                var response = await mediator.Send(command);
                return response.ToHttpResult();
            }).RequireRateLimiting("login-per-ip");

            auth.MapPost("/refresh", async (IMediator mediator, RefreshTokenCommand command) =>
            {
                var response = await mediator.Send(command);
                return response.ToHttpResult();
            }).RequireRateLimiting("refresh-per-ip");

            // Body carries the refresh token; no Bearer required so expired access tokens can still log out.
            auth.MapPost("/logout", async (IMediator mediator, LogoutCommand command) =>
            {
                var response = await mediator.Send(command);
                return response.ToHttpResult();
            });
        }
    }
}
