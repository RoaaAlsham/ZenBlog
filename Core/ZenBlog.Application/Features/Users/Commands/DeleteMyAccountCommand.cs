using MediatR;
using ZenBlog.Application.Base;

namespace ZenBlog.Application.Features.Users.Commands;

public sealed class DeleteMyAccountCommand : IRequest<BaseResult<bool>>
{
    public required string CurrentPassword { get; set; }
}
