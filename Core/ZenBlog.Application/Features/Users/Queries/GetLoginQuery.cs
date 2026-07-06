using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Queries
{
    public class GetLoginQuery : IRequest<BaseResult<GetLoginQueryResult>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
