namespace ZenBlog.Application.Contracts.Identity
{
    // Lets MediatR handlers ask "who is calling me?" without depending on
    // Microsoft.AspNetCore.Http (Application must stay framework-agnostic).
    // Implemented in Infrastructure using IHttpContextAccessor.
    public interface ICurrentUserService
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
