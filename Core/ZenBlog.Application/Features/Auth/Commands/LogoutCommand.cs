using MediatR;
using ZenBlog.Application.Base;

namespace ZenBlog.Application.Features.Auth.Commands;

public class LogoutCommand : IRequest<BaseResult<bool>>
{
    public string? RefreshToken { get; set; }
}
