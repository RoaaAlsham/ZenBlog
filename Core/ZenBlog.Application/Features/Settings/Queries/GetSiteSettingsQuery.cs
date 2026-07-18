using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Settings.Results;

namespace ZenBlog.Application.Features.Settings.Queries;

public record GetSiteSettingsQuery : IRequest<BaseResult<SiteSettingsResult>>;
