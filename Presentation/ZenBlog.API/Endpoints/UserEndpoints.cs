using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Queries;

namespace ZenBlog.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void RegisterUserEndpoints(this IEndpointRouteBuilder erb)
        {
            var users = erb.MapGroup("/users").WithTags("Users");
            users.MapPost("/register", async (IMediator mediator, CreateUserCommand command) =>
            {
                var result = await mediator.Send(command);
                return result.ToHttpResult();
            });

            users.MapGet("/", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery());
                return result.ToHttpResult();
            }).RequireAuthorization();
        }
    }
}
