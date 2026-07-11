using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class GetPublicUserByUsernameQueryHandler(UserManager<AppUser> userManager)
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

        var user = await userManager.FindByNameAsync(request.Username.Trim());
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
