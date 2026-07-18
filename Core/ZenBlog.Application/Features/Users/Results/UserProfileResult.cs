namespace ZenBlog.Application.Features.Users.Results;

public sealed record UserProfileResult(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? ImageUrl,
    string? ImagePublicId);
