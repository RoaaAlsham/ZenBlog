using ZenBlog.Application.Contracts.Identity;

namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Makes sure the reader the gateway just vouched for has a row in this service's
    /// own user table, keyed by their AuthDeep subject id.
    ///
    /// AuthDeep owns identity; this service still owns content, and a blog post needs an
    /// author row to point at. Provisioning has to happen before any handler runs, which
    /// is why it sits in the pipeline rather than inside the endpoints that happen to
    /// need it — one forgotten call site would be a foreign-key violation at write time.
    ///
    /// Runs only for verified human callers. An API key has no user to provision, and an
    /// anonymous read has nobody at all.
    /// </summary>
    public sealed class AuthDeepUserProvisioningMiddleware(
        RequestDelegate next,
        ILogger<AuthDeepUserProvisioningMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context, IAuthDeepUserProvisioner provisioner)
        {
            if (context.Items.TryGetValue(AuthDeepGatewayMiddleware.IdentityItemKey, out var stashed)
                && stashed is AuthDeepIdentity { IsHuman: true } identity)
            {
                try
                {
                    await provisioner.EnsureLocalUserAsync(
                        new AuthDeepUserDescriptor(identity.UserId!, identity.Email),
                        context.RequestAborted);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // A provisioning failure must not turn every read into a 500. Reads do
                    // not need the row; writes will fail loudly on the foreign key, which
                    // is the honest place for it to surface.
                    logger.LogError(
                        exception,
                        "Could not provision a local user for AuthDeep subject {AuthDeepId}",
                        identity.UserId);
                }
            }

            await next(context);
        }
    }
}
