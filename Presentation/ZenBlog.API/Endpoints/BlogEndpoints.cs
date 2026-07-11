using MediatR;
using ZenBlog.API.Extensions;
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
                return response.ToHttpResult();
            });

            blogs.MapPost("", async (IMediator _mediator, CreateBlogCommand command) =>
            {
               var response = await _mediator.Send(command);
                return response.ToHttpResult();
            }).RequireAuthorization();

            blogs.MapGet("/{id}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new GetBlogByIdQuery(id));
                return response.ToHttpResult();
            });

            blogs.MapPut("/{id}", async (IMediator _mediator, Guid id, UpdateBlogCommand command) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest("Id in URL does not match Id in request body.");
                }
                var response = await _mediator.Send(command);
                return response.ToHttpResult();
            }).RequireAuthorization();

            blogs.MapDelete("/{id}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new RemoveBlogCommand(id));
                return response.ToHttpDeleteResult();
            }).RequireAuthorization();

            blogs.MapGet("/category/{categoryId}", async (IMediator _mediator, Guid categoryId) =>
            {
                var response = await _mediator.Send(new GetBlogsByCategoryIdQuery(categoryId));
                return response.ToHttpResult();
            });

            blogs.MapGet("/user/{userId}", async (IMediator _mediator, string userId) =>
            {
                var response = await _mediator.Send(new GetBlogsByUserIdQuery(userId));
                return response.ToHttpResult();
            });
        }
    }
}
