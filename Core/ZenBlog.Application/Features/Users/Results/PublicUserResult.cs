namespace ZenBlog.Application.Features.Users.Results;

public sealed record PublicUserResult(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string? ImageUrl);
