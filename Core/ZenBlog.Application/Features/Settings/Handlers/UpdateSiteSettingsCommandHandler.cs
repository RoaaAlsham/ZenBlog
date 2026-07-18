using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Settings.Commands;
using ZenBlog.Application.Features.Settings.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Settings.Handlers;

public class UpdateSiteSettingsCommandHandler(
    IRepository<SiteSettings> repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    UserManager<AppUser> userManager)
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

        var caller = await userManager.FindByIdAsync(currentUser.UserId);
        var roles = caller is null ? [] : await userManager.GetRolesAsync(caller);
        var isAdmin = roles.Any(role =>
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));

        if (!isAdmin)
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

        return BaseResult<SiteSettingsResult>.Success(
            new SiteSettingsResult(settings.AllowRegistrations));
    }
}
