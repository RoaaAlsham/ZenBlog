using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Features.Auth.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Auth.Handlers;

public class RefreshTokenCommandHandler(
    UserManager<AppUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    IRefreshTokenService refreshTokenService,
    IRepository<RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, BaseResult<RefreshTokenResult>>
{
    public async Task<BaseResult<RefreshTokenResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        var incomingHash = refreshTokenService.HashToken(request.RefreshToken);
        var existingToken = await refreshTokenRepository.GetSingleAsync(
            rt => rt.TokenHash == incomingHash,
            cancellationToken);

        if (existingToken is null ||
            existingToken.RevokedAtUtc is not null ||
            existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId);
        if (user is null)
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessTokenExpiresAtUtc) = tokenGenerator.GenerateToken(user, roles, 15);
        var (newRefreshToken, newRefreshTokenHash, newRefreshTokenExpiresAtUtc) = refreshTokenService.GenerateRefreshToken(7);

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = newRefreshTokenHash;

        await refreshTokenRepository.UpdateAsync(existingToken);
        await refreshTokenRepository.CreateAsync(new RefreshToken
        {
            TokenHash = newRefreshTokenHash,
            UserId = existingToken.UserId,
            ExpiresAtUtc = newRefreshTokenExpiresAtUtc
        });

        var saved = await unitOfWork.SaveChangesAsync();
        if (!saved)
        {
            return BaseResult<RefreshTokenResult>.Failure("Failed to refresh token.");
        }

        return BaseResult<RefreshTokenResult>.Success(
            new RefreshTokenResult(accessToken, accessTokenExpiresAtUtc, newRefreshToken, newRefreshTokenExpiresAtUtc));
    }
}
