using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ZenBlog.API.IntegrationTests.Helpers;

namespace ZenBlog.API.IntegrationTests.Endpoints;

/// <summary>
/// Covers the temporary gateway-identity diagnostic.
///
/// The point of these tests is trust in the instrument: when the endpoint later runs behind
/// the real gateway, its output is only useful if we know it reports exactly what arrived —
/// neither inventing identity nor hiding a header the middleware does not read. Delete
/// alongside <c>GatewayDiagnosticsEndpoints</c>.
/// </summary>
public class GatewayDiagnosticsTests
{
    private const string Route = "/api/diagnostics/gateway-identity";

    /// <summary>Gateway factory with the diagnostic switched on.</summary>
    private sealed class DiagnosticsEnabledFactory : AuthDeepGatewayFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Diagnostics:GatewayIdentity"] = "true"
                });
            });
        }
    }

    [Fact]
    public async Task Disabled_ByDefault()
    {
        using var factory = new AuthDeepGatewayFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);

        var response = await client.SendAsync(request);

        // Not registered at all when the flag is absent — it cannot ship on by accident.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithoutGatewaySignature_IsRejected()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Route);

        // Protected by AuthDeepProtectedRoutes: reachable only through the gateway.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportsIdentityHeadersExactlyAsReceived()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        const string userId = "4f194b79-7a1b-4fea-be9e-a994d2846fee";

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: userId,
                roles: "admin,editor");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.GetProperty("signatureVerified").GetBoolean());

        var identity = body.GetProperty("parsedIdentity");
        Assert.Equal(userId, identity.GetProperty("userId").GetString());
        Assert.Equal($"{userId}@authdeep.test", identity.GetProperty("email").GetString());

        var roles = identity.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray();
        Assert.Equal(["admin", "editor"], roles);

        // Roles arrive lowercase from AuthDeep, which is precisely the casing mismatch the
        // auth scheme has to absorb before RequireRole("Admin") can ever match.
        Assert.DoesNotContain("Admin", roles);

        // Raw headers are echoed so anything the middleware does not parse is still visible.
        var forwarded = body.GetProperty("forwardedHeaders");
        Assert.Equal(userId, forwarded.GetProperty("X-AuthDeep-User-ID").GetString());

        // No end-user auth scheme exists yet — this is the gap the bridge closes.
        Assert.False(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task NeverEchoesGatewayCredentials()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret, userId: "u1");

        var response = await client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        // Presence is reported; the values are not, so a diagnostic response is safe to paste.
        Assert.DoesNotContain(AuthDeepGatewayFactory.GatewayKey, raw);
        Assert.DoesNotContain(AuthDeepGatewayFactory.ServiceSecret, raw);
        Assert.Contains("<present,", raw);
    }

    [Fact]
    public async Task WhenGatewaySendsNoUserIdentity_ReportsItAsAbsent()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        // Signed as a service with no end-user headers — the exact shape expected when the
        // caller authenticates with sak_ and the gateway has no reader to name.
        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.GetProperty("signatureVerified").GetBoolean());
        // Verified transport, no actor: the distinction the whole bridge design turns on.
        Assert.Equal(JsonValueKind.Null, body.GetProperty("parsedIdentity").GetProperty("userId").ValueKind);
    }
}
