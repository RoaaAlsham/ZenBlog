using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Auth.Results;

namespace ZenBlog.Application.Features.Auth.Commands
{
    public class LoginCommand : IRequest<BaseResult<LoginResult>>
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
