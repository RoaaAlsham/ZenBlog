using MediatR;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Queries;
namespace ZenBlog.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void RegisterUserEndpoints(this IEndpointRouteBuilder erb)
        {
            // Only registration and login should be anonymous endpoint,
            // everything else should inherit the .RequireAuthorization() applied to
            // the "/api" group in Program.cs. 

            var users = erb.MapGroup("/users").WithTags("Users");

            users.MapPost("/register", async (CreateUserCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            }).AllowAnonymous();

            users.MapPost("/login", async (GetLoginQuery query, IMediator mediator) =>
            {
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            }).AllowAnonymous();

            // No .AllowAnonymous() here on purpose: listing users should require a valid bearer token.
            users.MapGet("/", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery());
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Errors);
            });
        }
    }
}
