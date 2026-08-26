namespace ZenBlog.Application.Contracts.Identity;

/// <summary>
/// Reads the local Identity role table, which now serves exactly one caller: the legacy
/// ZenBlog JWT endpoints, which mint their own token and must stamp roles into it.
///
/// Authorization does NOT go through here. AuthDeep asserts the caller's roles on every
/// gateway-forwarded request and <see cref="ICurrentUserService.IsAdmin"/> reads them;
/// this table holds nothing for an AuthDeep reader, so a check against it answers "not an
/// admin" for a tenant admin. That was a live bug — a tenant admin refused on every
/// non-owner delete — which is why the ask-if-someone-is-an-admin methods are gone from
/// this interface rather than left available to the next handler.
/// </summary>
public interface IRoleChecker
{
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
}
