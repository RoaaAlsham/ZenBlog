using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Queries;

public sealed class GetCurrentUserQuery : IRequest<BaseResult<UserProfileResult>>;
