using MediatR;
using ZenBlog.Application.Features.Blogs.Queries;
using ZenBlog.Application.Features.Blogs.Commands;
namespace ZenBlog.API.Endpoints
{
    public static class BlogEndpoints
    {
        public static void RegisterBlogEndpoints(this IEndpointRouteBuilder erb)
        {
             var blogs = erb.MapGroup("/blogs").WithTags("Blogs");

            blogs.MapGet("", async (IMediator _mediator) =>
            {
                var response = await _mediator.Send(new GetBlogsQuery());
               return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            });

            blogs.MapPost("", async (IMediator _mediator, CreateBlogCommand command) =>
            {
               var response = await _mediator.Send(command);
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);        
            }).RequireAuthorization();

            blogs.MapGet("/{id}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new GetBlogByIdQuery(id));
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            });

            blogs.MapPut("/{id}", async (IMediator _mediator, Guid id, UpdateBlogCommand command) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest("Id in URL does not match Id in request body.");
                }
                var response = await _mediator.Send(command);
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            }).RequireAuthorization();

            blogs.MapDelete("/{id}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new RemoveBlogCommand(id));
                return response.IsSuccess ? Results.Ok() : Results.BadRequest(response.Errors);
            }).RequireAuthorization();

            blogs.MapGet("/category/{categoryId}", async (IMediator _mediator, Guid categoryId) =>
            {
                var response = await _mediator.Send(new GetBlogsByCategoryIdQuery(categoryId));
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            });
        }
    }
}
