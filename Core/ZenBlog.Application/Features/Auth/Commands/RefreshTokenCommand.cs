using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Auth.Results;

namespace ZenBlog.Application.Features.Auth.Commands;

public class RefreshTokenCommand : IRequest<BaseResult<RefreshTokenResult>>
{
    public string RefreshToken { get; set; } = default!;
}
