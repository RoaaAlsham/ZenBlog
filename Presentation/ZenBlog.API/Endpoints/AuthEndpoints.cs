using MediatR;
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
                // 401, not 400: this failure means "who you claim to be is not accepted",
                // which is what Unauthorized communicates to API clients.
                return response.IsSuccess ? Results.Ok(response.Data) : Results.Unauthorized();
            });
        }
    }
}
