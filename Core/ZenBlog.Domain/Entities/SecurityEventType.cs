namespace ZenBlog.Domain.Entities;

public enum SecurityEventType
{
    LoginSuccess = 0,
    LoginFailure = 1,
    RateLimited = 2
}
