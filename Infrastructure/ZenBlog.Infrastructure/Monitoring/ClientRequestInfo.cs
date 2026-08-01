using Microsoft.AspNetCore.Http;
using ZenBlog.Application.Contracts.Monitoring;

namespace ZenBlog.Infrastructure.Monitoring;

public sealed class ClientRequestInfo(IHttpContextAccessor httpContextAccessor) : IClientRequestInfo
{
    public string SourceIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public string Host =>
        httpContextAccessor.HttpContext?.Request.Host.Value ?? "unknown";

    public string Path
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "unknown";
            }

            var path = context.Request.Path.Value ?? "/";
            var query = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value
                : string.Empty;
            return path + query;
        }
    }
}
