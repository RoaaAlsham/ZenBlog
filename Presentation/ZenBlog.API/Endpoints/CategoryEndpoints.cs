using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Categories.Queries;
using ZenBlog.Application.Features.Categories.Commands;

namespace ZenBlog.API.Endpoints
{
    public static class CategoryEndpoints
    {
        public static void RegisterCategoryEndpoints(this IEndpointRouteBuilder erb)
        {
            var categories = erb.MapGroup("/categories").WithTags("Categories");
            categories.MapGet("", async (IMediator _mediator) =>
            {
                var response = await _mediator.Send(new GetCategoryQuery());
                return response.ToHttpResult();
            });

            categories.MapPost("", async (IMediator _mediator, CreateCategoryCommand command) =>
            {
                var response = await _mediator.Send(command);
                return response.IsSuccess
                    ? Results.Created($"/categories/{response.Data}", null)
                    : response.ToHttpResult();
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));

            categories.MapGet("/{id}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new GetCategoryByIdQuery(id));
                return response.ToHttpResult();
            });

            categories.MapPut("/{id:guid}", async (IMediator _mediator, Guid id, UpdateCategoryCommand command) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest("ID in the URL does not match ID in the body.");
                }
                var response = await _mediator.Send(command);
                return response.ToHttpNoContentResult();
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));

            categories.MapDelete("/{id:guid}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new RemoveCategoryCommand(id));
                return response.ToHttpNoContentResult();
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }
}
