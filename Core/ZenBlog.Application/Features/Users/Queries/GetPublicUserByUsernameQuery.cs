using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Queries;

public sealed record GetPublicUserByUsernameQuery(string Username)
    : IRequest<BaseResult<PublicUserResult>>;
