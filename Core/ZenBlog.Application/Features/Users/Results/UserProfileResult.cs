namespace ZenBlog.Application.Features.Users.Results;

/// <param name="Roles">
/// Roles as AuthDeep asserted them for this request, not as this service stores them.
/// Only populated when answering "who am I?" — the caller's own roles are the only ones
/// the gateway tells us about. Null elsewhere, which is why it is last and optional.
/// </param>
public sealed record UserProfileResult(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? ImageUrl,
    string? ImagePublicId,
    IReadOnlyList<string>? Roles = null);
