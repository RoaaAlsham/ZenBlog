using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Auth.Handlers
{
    public class LoginCommandHandler(
        IUserQueryService userQuery,
        IUserAccountService userAccount,
        IRoleChecker roleChecker,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenService refreshTokenService,
        IRepository<RefreshToken> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ISecurityRequestLogger securityRequestLogger) : IRequestHandler<LoginCommand, BaseResult<LoginResult>>
    {
        public async Task<BaseResult<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await userQuery.FindByEmailAsync(request.Email, cancellationToken);
            if (user is null)
            {
                // Deliberately the SAME message as a wrong password below:
                // never reveal whether the email exists (avoids user-enumeration attacks).
                await securityRequestLogger.LogAsync(
                    SecurityEventType.LoginFailure,
                    statusCode: 401,
                    cancellationToken: cancellationToken);
                return BaseResult<LoginResult>.Unauthorized("Invalid email or password.");
            }

            var passwordValid = await userAccount.CheckPasswordAsync(user, request.Password, cancellationToken);
            if (!passwordValid)
            {
                await securityRequestLogger.LogAsync(
                    SecurityEventType.LoginFailure,
                    statusCode: 401,
                    cancellationToken: cancellationToken);
                return BaseResult<LoginResult>.Unauthorized("Invalid email or password.");
            }

            var roles = (await roleChecker.GetRolesAsync(user.Id, cancellationToken)).ToList();
            // Expiry comes from JwtSettings via the generator default (not a hardcoded minutes value).
            var (token, expiresAtUtc) = tokenGenerator.GenerateToken(user, roles);
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

            await securityRequestLogger.LogAsync(
                SecurityEventType.LoginSuccess,
                statusCode: 200,
                cancellationToken: cancellationToken);

            return BaseResult<LoginResult>.Success(
                new LoginResult(
                    user.Id,
                    user.Email!,
                    user.UserName!,
                    user.FirstName,
                    user.LastName,
                    user.ImageUrl,
                    token,
                    expiresAtUtc,
                    refreshToken,
                    refreshTokenExpiresAtUtc));
        }
    }
}
