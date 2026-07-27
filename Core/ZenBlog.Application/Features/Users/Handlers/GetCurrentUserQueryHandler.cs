using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class GetCurrentUserQueryHandler(
    IUserQueryService userQuery,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, BaseResult<UserProfileResult>>
{
    public async Task<BaseResult<UserProfileResult>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<UserProfileResult>.Unauthorized("You are not authenticated.");
        }

        var user = await userQuery.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return BaseResult<UserProfileResult>.NotFound("User not found.");
        }

        return BaseResult<UserProfileResult>.Success(ToProfileResult(user));
    }

    internal static UserProfileResult ToProfileResult(AppUser user) =>
        new(
            Id: user.Id,
            Username: user.UserName!,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            ImageUrl: user.ImageUrl,
            ImagePublicId: user.ImagePublicId);
}
