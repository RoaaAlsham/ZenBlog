// CommentEndpoints.cs
using MediatR;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Queries;

namespace ZenBlog.API.Endpoints
{
    public static class CommentEndpoints
    {
        public static void RegisterCommentEndpoints(this IEndpointRouteBuilder erb)
        {
            var comments = erb.MapGroup("/comments").WithTags("Comments");

            // Get all top-level comments for a blog
            comments.MapGet("/blog/{blogId}", async (IMediator mediator, Guid blogId) =>
            {
                var response = await mediator.Send(new GetCommentsByBlogIdQuery(blogId));
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            });

            // Get single comment with its replies
            comments.MapGet("/{id}", async (IMediator mediator, Guid id) =>
            {
                var response = await mediator.Send(new GetCommentByIdQuery(id));
                return response.IsSuccess ? Results.Ok(response.Data) : Results.NotFound(response.Errors);
            });

            // Create top-level comment or reply
            comments.MapPost("", async (IMediator mediator, CreateCommentCommand command) =>
            {
                var response = await mediator.Send(command);
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            }).RequireAuthorization();

            // Update comment body only
            comments.MapPut("/{id}", async (IMediator mediator, Guid id, UpdateCommentCommand command) =>
            {
                if (id != command.Id)
                    return Results.BadRequest("Id in URL does not match Id in body.");

                var response = await mediator.Send(command);
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            }).RequireAuthorization();

            // Delete comment
            comments.MapDelete("/{id}", async (IMediator mediator, Guid id) =>
            {
                var response = await mediator.Send(new RemoveCommentCommand(id));
                return response.IsSuccess ? Results.Ok() : Results.BadRequest(response.Errors);
            }).RequireAuthorization();
        }
    }
}