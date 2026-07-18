using MediatR;
using Microsoft.AspNetCore.Mvc;
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

            users.MapGet("/me", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCurrentUserQuery());
                return result.ToHttpResult();
            }).RequireAuthorization();

            users.MapPut("/me", async (IMediator mediator, UpdateProfileCommand command) =>
            {
                var result = await mediator.Send(command);
                return result.ToHttpResult();
            }).RequireAuthorization();

            users.MapPut("/me/password", async (IMediator mediator, ChangePasswordCommand command) =>
            {
                var result = await mediator.Send(command);
                return result.ToHttpResult();
            }).RequireAuthorization();

            users.MapDelete("/me", async (IMediator mediator, [FromBody] DeleteMyAccountCommand command) =>
            {
                var result = await mediator.Send(command);
                return result.ToHttpNoContentResult();
            }).RequireAuthorization();

            users.MapGet("/by-username/{username}", async (IMediator mediator, string username) =>
            {
                var result = await mediator.Send(new GetPublicUserByUsernameQuery(username));
                return result.ToHttpResult();
            });

            users.MapGet("/", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery());
                return result.ToHttpResult();
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));

            users.MapDelete("/{id}", async (IMediator mediator, string id) =>
            {
                var result = await mediator.Send(new DeleteUserCommand(id));
                return result.ToHttpNoContentResult();
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }
}
