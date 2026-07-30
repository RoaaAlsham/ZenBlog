using MediatR;
using ZenBlog.Application.Base;

namespace ZenBlog.Application.Features.Users.Commands;

public sealed record PromoteUserToAdminCommand(string Id) : IRequest<BaseResult<bool>>;
