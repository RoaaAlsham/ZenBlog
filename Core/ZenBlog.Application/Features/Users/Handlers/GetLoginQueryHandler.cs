
using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;
using ZenBlog.Application.Contracts;
using AutoMapper;

namespace ZenBlog.Application.Features.Users.Handlers
{
    public class GetLoginQueryHandler(
        UserManager<AppUser> userManager,
        IJwtService tokenService,
        IMapper mapper) : IRequestHandler<GetLoginQuery, BaseResult<GetLoginQueryResult>>
    {
        public async Task<BaseResult<GetLoginQueryResult>> Handle(GetLoginQuery request, CancellationToken cancellationToken)
        {
            //Two different messages let an attacker enumerate which emails are
            // registered simply by trying logins and reading the response,
            //so both cases must indistinguishable to the caller.

            const string invalidCredentialsMessage = "Invalid email or password.";

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null) {
                return BaseResult<GetLoginQueryResult>.Failure(invalidCredentialsMessage);
            }
            var result = await userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                return BaseResult<GetLoginQueryResult>.Failure(invalidCredentialsMessage);
            }

            var userResult = mapper.Map<GetAllUsersQueryResult>(user);
            var response = await tokenService.GenerateJwtTokenAsync(userResult);
            return BaseResult<GetLoginQueryResult>.Success(response);
        }
    }
}
