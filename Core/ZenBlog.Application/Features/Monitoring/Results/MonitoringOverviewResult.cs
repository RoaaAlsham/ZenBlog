namespace ZenBlog.Application.Features.Monitoring.Results;

public sealed record MonitoringOverviewResult(
    int NewUsersLast24Hours,
    int NewUsersLast30Days,
    int NewBlogsLast24Hours,
    int NewBlogsLast30Days,
    int NewCommentsLast24Hours,
    int NewCommentsLast30Days,
    int FailedLoginsLast24Hours,
    int RateLimitHitsLast24Hours);
