namespace ZenBlog.Application.Features.Users.Results
{
    public sealed record GetAllUsersQueryResult(
        string Id,
        string Username,
        string Email,
        string FullName,
        string? ImageUrl,
        bool IsAdmin
    );
}
