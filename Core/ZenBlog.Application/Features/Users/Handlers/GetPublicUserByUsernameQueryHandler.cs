using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Handlers;

public class GetPublicUserByUsernameQueryHandler(IUserQueryService userQuery)
    : IRequestHandler<GetPublicUserByUsernameQuery, BaseResult<PublicUserResult>>
{
    public async Task<BaseResult<PublicUserResult>> Handle(
        GetPublicUserByUsernameQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BaseResult<PublicUserResult>.NotFound("User not found.");
        }

        var user = await userQuery.FindByUserNameAsync(request.Username.Trim(), cancellationToken);
        if (user is null)
        {
            return BaseResult<PublicUserResult>.NotFound("User not found.");
        }

        return BaseResult<PublicUserResult>.Success(new PublicUserResult(
            Id: user.Id,
            Username: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            ImageUrl: user.ImageUrl));
    }
}
