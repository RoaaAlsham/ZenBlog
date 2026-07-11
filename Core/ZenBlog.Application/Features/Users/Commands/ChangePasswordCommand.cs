using MediatR;
using ZenBlog.Application.Base;

namespace ZenBlog.Application.Features.Users.Commands;

public sealed class ChangePasswordCommand : IRequest<BaseResult<bool>>
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}
