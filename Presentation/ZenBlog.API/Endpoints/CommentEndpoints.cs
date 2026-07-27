using MediatR;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Queries;

namespace ZenBlog.API.Endpoints
{
    public static class CommentEndpoints
    {
        public static void RegisterCommentEndpoints(this IEndpointRouteBuilder erb)
        {
            var comments = erb.MapGroup("/comments").WithTags("Comments");

            comments.MapGet("/blog/{blogId}", async (
                IMediator mediator,
                Guid blogId,
                int? page,
                int? pageSize) =>
            {
                var response = await mediator.Send(
                    new GetCommentsByBlogIdQuery(blogId, page, pageSize));
                return response.ToHttpResult();
            });

            comments.MapGet("/{id}", async (IMediator mediator, Guid id) =>
            {
                var response = await mediator.Send(new GetCommentByIdQuery(id));
                return response.ToHttpResult();
            });

            comments.MapPost("", async (IMediator mediator, CreateCommentCommand command) =>
            {
                var response = await mediator.Send(command);
                return response.ToHttpResult();
            }).RequireAuthorization();

            comments.MapPut("/{id}", async (IMediator mediator, Guid id, UpdateCommentCommand command) =>
            {
                if (id != command.Id)
                    return Results.BadRequest("Id in URL does not match Id in body.");

                var response = await mediator.Send(command);
                return response.ToHttpResult();
            }).RequireAuthorization();

            comments.MapDelete("/{id}", async (IMediator mediator, Guid id) =>
            {
                var response = await mediator.Send(new RemoveCommentCommand(id));
                return response.ToHttpDeleteResult();
            }).RequireAuthorization();
        }
    }
}
