using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Settings.Queries;
using ZenBlog.Application.Features.Settings.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Settings.Handlers;

public class GetSiteSettingsQueryHandler(
    IRepository<SiteSettings> repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetSiteSettingsQuery, BaseResult<SiteSettingsResult>>
{
    public async Task<BaseResult<SiteSettingsResult>> Handle(
        GetSiteSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await SiteSettingsAccess.GetOrCreateAsync(
            repository,
            unitOfWork,
            cancellationToken);

        return BaseResult<SiteSettingsResult>.Success(
            new SiteSettingsResult(settings.AllowRegistrations));
    }
}
