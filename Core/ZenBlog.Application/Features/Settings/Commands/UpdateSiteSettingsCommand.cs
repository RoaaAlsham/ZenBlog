using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Settings.Results;

namespace ZenBlog.Application.Features.Settings.Commands;

public record UpdateSiteSettingsCommand(bool AllowRegistrations)
    : IRequest<BaseResult<SiteSettingsResult>>;
