using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Application.Features.Settings.Commands;
using ZenBlog.Application.Features.Settings.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Settings.Handlers;

public class UpdateSiteSettingsCommandHandler(
    IRepository<SiteSettings> repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IRoleChecker roleChecker,
    IUserQueryService userQuery,
    IActivityLogger activityLogger)
    : IRequestHandler<UpdateSiteSettingsCommand, BaseResult<SiteSettingsResult>>
{
    public async Task<BaseResult<SiteSettingsResult>> Handle(
        UpdateSiteSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<SiteSettingsResult>.Unauthorized(
                "You must be signed in to update site settings.");
        }

        if (!await roleChecker.IsInRoleAsync(currentUser.UserId, "Admin", cancellationToken))
        {
            return BaseResult<SiteSettingsResult>.Forbidden(
                "Only administrators can update site settings.");
        }

        var settings = await SiteSettingsAccess.GetOrCreateAsync(
            repository,
            unitOfWork,
            cancellationToken);

        settings.AllowRegistrations = request.AllowRegistrations;
        settings.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(settings);
        var saved = await unitOfWork.SaveChangesAsync();
        if (!saved)
        {
            return BaseResult<SiteSettingsResult>.Failure("Failed to update site settings.");
        }

        var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
        await activityLogger.LogAsync(
            ActivityActions.SettingsUpdated,
            $"Updated site settings (allowRegistrations={settings.AllowRegistrations})",
            actorId,
            actorName,
            nameof(SiteSettings),
            settings.Id.ToString(),
            cancellationToken: cancellationToken);

        return BaseResult<SiteSettingsResult>.Success(
            new SiteSettingsResult(settings.AllowRegistrations));
    }
}
