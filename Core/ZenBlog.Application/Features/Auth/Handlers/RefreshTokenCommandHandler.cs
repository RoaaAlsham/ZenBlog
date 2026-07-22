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

        if (existingToken is null)
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        // Reuse detection: a revoked token being presented again usually means a thief
        // already rotated it. Kill the whole refresh-token family for that user.
        if (existingToken.RevokedAtUtc is not null)
        {
            await RevokeAllTokensForUserAsync(existingToken.UserId, cancellationToken);
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        if (existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId);
        if (user is null)
        {
            return BaseResult<RefreshTokenResult>.Unauthorized("Invalid refresh token.");
        }

        var roles = await userManager.GetRolesAsync(user);
        // Expiry comes from JwtSettings via the generator default (not a hardcoded minutes value).
        var (accessToken, accessTokenExpiresAtUtc) = tokenGenerator.GenerateToken(user, roles);
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

    private async Task RevokeAllTokensForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var tokens = await refreshTokenRepository.GetAllWithIncludesAsync(
            rt => rt.UserId == userId && rt.RevokedAtUtc == null,
            cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            await refreshTokenRepository.UpdateAsync(token);
        }

        if (tokens.Count > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
