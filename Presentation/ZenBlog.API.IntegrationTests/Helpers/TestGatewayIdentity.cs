using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.API.CustomMiddlewares;
using ZenBlog.Domain.Entities;

namespace ZenBlog.API.IntegrationTests.Helpers;

/// <summary>
/// Stands in for the AuthDeep gateway so tests can act as a signed-in reader.
///
/// Human identity now reaches this service in exactly one way: injected by the gateway
/// and vouched for by an HMAC over the whole request. Tests that only want to assert
/// "an admin can delete a category" have no business reproducing that signature on every
/// call, so this filter short-circuits the *transport* while keeping the *shape* — it
/// stashes the same <see cref="AuthDeepIdentity"/> in the same slot the real middleware
/// uses, and everything downstream is the production path.
///
/// This lives in the test assembly on purpose. Nothing in ZenBlog.API can turn it on, so
/// there is no configuration mistake that could make a deployed service trust a header
/// like this. Tests that exercise signature verification itself use
/// <see cref="AuthDeepGatewayFactory"/> and sign for real.
///
/// Roles default to the local Identity store, which is what AssignRoleAsync writes, but
/// <see cref="RolesHeader"/> overrides them. The override is the honest case: in
/// production AuthDeep asserts roles for a reader with no local role row at all, and a
/// test that can only make an admin by writing one cannot notice a check that reads the
/// local table instead of the request.
/// </summary>
public sealed class TestGatewayIdentityFilter : IStartupFilter
{
    /// <summary>Carries the acting user's id. Deliberately not an Authorization header.</summary>
    public const string UserIdHeader = "X-Test-Gateway-User-Id";

    /// <summary>Comma-separated roles AuthDeep asserts, in its own vocabulary.</summary>
    public const string RolesHeader = "X-Test-Gateway-User-Roles";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            var userId = context.Request.Headers[UserIdHeader].ToString();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId);

                if (user is not null)
                {
                    var declared = context.Request.Headers[RolesHeader].ToString();
                    var roles = string.IsNullOrWhiteSpace(declared)
                        ? (IReadOnlyList<string>)(await userManager.GetRolesAsync(user)).ToArray()
                        : declared.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    context.Items[AuthDeepGatewayMiddleware.IdentityItemKey] = new AuthDeepIdentity(
                        AuthType: AuthDeepAuthType.WebToken,
                        UserId: user.Id,
                        TenantId: "tenant-under-test",
                        Email: user.Email,
                        Roles: roles,
                        ApiKeyId: null,
                        ApiKeyType: null,
                        RequestId: Guid.NewGuid().ToString());
                }
            }

            await nextMiddleware(context);
        });

        next(app);
    };
}
