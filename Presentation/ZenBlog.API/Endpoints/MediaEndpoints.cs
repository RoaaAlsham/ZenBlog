using MediatR;
using Microsoft.AspNetCore.Mvc;
using ZenBlog.API.Extensions;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Media.Commands;

namespace ZenBlog.API.Endpoints;

public static class MediaEndpoints
{
    public static void RegisterMediaEndpoints(this IEndpointRouteBuilder erb)
    {
        var media = erb.MapGroup("/media").WithTags("Media");

        media.MapPost("/images", async (
                IMediator mediator,
                IFormFile file,
                [FromForm] ImageUploadPurpose purpose) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { errors = new[] { new { errorMessage = "A file is required." } } });
                }

                await using var stream = file.OpenReadStream();
                var result = await mediator.Send(new UploadImageCommand
                {
                    Purpose = purpose,
                    Content = stream,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length
                });

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(ImageUploadLimits.MultipartHardLimitBytes))
            .Accepts<IFormFile>("multipart/form-data");
    }
}
