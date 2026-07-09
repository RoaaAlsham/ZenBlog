using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Auth.Handlers
{
    public class LoginCommandHandler(
        UserManager<AppUser> userManager,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenService refreshTokenService,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, BaseResult<LoginResult>>
    {
        public async Task<BaseResult<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                // Deliberately the SAME message as a wrong password below:
                // never reveal whether the email exists (avoids user-enumeration attacks).
                return BaseResult<LoginResult>.Unauthorized("Invalid email or password.");
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return BaseResult<LoginResult>.Unauthorized("Invalid email or password.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = tokenGenerator.GenerateToken(user, roles, 15);
            var (refreshToken, refreshTokenHash, refreshTokenExpiresAtUtc) = refreshTokenService.GenerateRefreshToken(7);
            await refreshTokenRepository.CreateAsync(new RefreshToken
            {
                TokenHash = refreshTokenHash,
                UserId = user.Id,
                ExpiresAtUtc = refreshTokenExpiresAtUtc
            });
            var saved = await unitOfWork.SaveChangesAsync();
            if (!saved)
            {
                return BaseResult<LoginResult>.Failure("Failed to complete login.");
            }

            return BaseResult<LoginResult>.Success(
                new LoginResult(user.Id, user.Email!, token, expiresAtUtc, refreshToken, refreshTokenExpiresAtUtc));
        }
    }
}
