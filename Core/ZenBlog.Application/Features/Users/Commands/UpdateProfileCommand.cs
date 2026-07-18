using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Commands;

public sealed class UpdateProfileCommand : IRequest<BaseResult<UserProfileResult>>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
}
