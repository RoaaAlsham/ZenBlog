
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
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null) {
                return BaseResult<GetLoginQueryResult>.Failure("User not found.");
            }
            var result = await userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                return BaseResult<GetLoginQueryResult>.Failure("Invalid email or password.");// dont specify which one is invalid for security reasons
            }

            var userResult = mapper.Map<GetAllUsersQueryResult>(user);
            var response = await tokenService.GenerateJwtTokenAsync(userResult);
            return BaseResult<GetLoginQueryResult>.Success(response);
        }
    }
}
