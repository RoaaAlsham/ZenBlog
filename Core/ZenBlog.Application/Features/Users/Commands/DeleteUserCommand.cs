using MediatR;
using ZenBlog.Application.Base;

namespace ZenBlog.Application.Features.Users.Commands;

public sealed record DeleteUserCommand(string Id) : IRequest<BaseResult<bool>>;
