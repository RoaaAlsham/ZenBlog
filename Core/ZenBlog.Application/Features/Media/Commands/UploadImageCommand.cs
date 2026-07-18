using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Media.Results;

namespace ZenBlog.Application.Features.Media.Commands;

public sealed class UploadImageCommand : IRequest<BaseResult<UploadImageResult>>
{
    public required ImageUploadPurpose Purpose { get; init; }
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
}
