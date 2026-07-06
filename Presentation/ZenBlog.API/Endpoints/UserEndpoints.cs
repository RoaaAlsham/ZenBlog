using MediatR;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Queries;
namespace ZenBlog.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void RegisterUserEndpoints(this IEndpointRouteBuilder erb)
        {
            var users = erb.MapGroup("/users").WithTags("Users").AllowAnonymous();
            users.MapPost("/register", async (CreateUserCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            });

            users.MapGet("/", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery());
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            });

            users.MapPost("/login", async (GetLoginQuery query, IMediator mediator) =>
            {
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            });




        }
    }
}
