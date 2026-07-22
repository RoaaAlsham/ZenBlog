using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Auth.Handlers;

public class LogoutCommandHandler(
    IRefreshTokenService refreshTokenService,
    IRepository<RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Always succeed so callers cannot probe whether a refresh token exists.
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BaseResult<bool>.Success(true);
        }

        var hash = refreshTokenService.HashToken(request.RefreshToken);
        var existing = await refreshTokenRepository.GetSingleAsync(
            rt => rt.TokenHash == hash,
            cancellationToken);

        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await refreshTokenRepository.UpdateAsync(existing);
            await unitOfWork.SaveChangesAsync();
        }

        return BaseResult<bool>.Success(true);
    }
}
