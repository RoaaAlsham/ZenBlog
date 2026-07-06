using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Auth.Handlers
{
    public class LoginCommandHandler(
        UserManager<AppUser> userManager,
        IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, BaseResult<LoginResult>>
    {
        public async Task<BaseResult<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                // Deliberately the SAME message as a wrong password below:
                // never reveal whether the email exists (avoids user-enumeration attacks).
                return BaseResult<LoginResult>.Failure("Invalid email or password.");
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return BaseResult<LoginResult>.Failure("Invalid email or password.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = tokenGenerator.GenerateToken(user, roles);

            return BaseResult<LoginResult>.Success(
                new LoginResult(user.Id, user.Email!, token, expiresAtUtc));
        }
    }
}
