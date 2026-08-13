namespace ZenBlog.Application.Contracts.Identity
{
    /// <summary>
    /// The reader as AuthDeep describes them, taken from the gateway-injected headers
    /// after the request signature has been verified.
    /// </summary>
    /// <param name="UserId">AuthDeep subject id. This becomes the local AppUser.Id.</param>
    /// <param name="Email">May be absent; the tenant decides which claims it releases.</param>
    public sealed record AuthDeepUserDescriptor(string UserId, string? Email);

    /// <summary>
    /// Makes sure a local AppUser row exists whose primary key IS the AuthDeep subject id.
    ///
    /// Blogs, comments and refresh tokens are all keyed on AppUser.Id, so a reader who
    /// exists only at AuthDeep cannot author anything until they exist here too. Readers
    /// who predate the AuthDeep integration hold a locally generated id and are re-keyed
    /// on their first signed-in request, matched by email, so their existing posts stay
    /// attached to them.
    /// </summary>
    public interface IAuthDeepUserProvisioner
    {
        /// <summary>
        /// Idempotent. Safe to call on every request; implementations are expected to make
        /// the common "already provisioned" case cheap.
        /// </summary>
        Task EnsureLocalUserAsync(
            AuthDeepUserDescriptor descriptor,
            CancellationToken cancellationToken = default);
    }
}
