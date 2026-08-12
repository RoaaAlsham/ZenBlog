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
/// The point of these tests is trust in the instrument: when the endpoint runs behind the
/// real gateway, its output is only worth acting on if we know it reports exactly what
/// arrived — neither inventing identity, nor hiding a header, nor quietly dropping one
/// whose name we failed to predict. Delete alongside
/// <c>GatewayDiagnosticsEndpoints</c>.
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

        var roles = identity.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray();
        Assert.Equal(["admin", "editor"], roles);

        // Roles arrive lowercase from AuthDeep, which is precisely the casing mismatch the
        // auth scheme has to absorb before RequireRole("Admin") can ever match.
        Assert.DoesNotContain("Admin", roles);

        Assert.Equal(userId, body.GetProperty("headers").GetProperty("X-AuthDeep-User-ID").GetString());

        // No end-user auth scheme exists yet — this is the gap the bridge closes.
        Assert.False(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    /// <summary>
    /// The assertion P0 actually turns on. An allowlisted view cannot tell "the gateway
    /// stripped this header" apart from "the endpoint never looked for it", so the report
    /// has to cover names nobody predicted — including namespaces of our own invention.
    /// </summary>
    [Fact]
    public async Task ReportsHeadersOutsideTheAuthDeepNamespace()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);
        request.Headers.Add("X-ZenBlog-Actor-Id", "actor-42");
        request.Headers.Add("X-Some-Unrelated-Header", "kept");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var headers = body.GetProperty("headers");

        Assert.Equal("actor-42", headers.GetProperty("X-ZenBlog-Actor-Id").GetString());
        Assert.Equal("kept", headers.GetProperty("X-Some-Unrelated-Header").GetString());

        // Anything absent from the live report is therefore genuinely absent from the wire.
        Assert.True(body.GetProperty("headerCount").GetInt32() >= 2);
    }

    [Fact]
    public async Task FlagsHeadersThatArrivedMoreThanOnce()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);
        request.Headers.Add("X-AuthDeep-User-ID", "first");
        request.Headers.Add("X-AuthDeep-User-ID", "second");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // A duplicate arrives comma-joined and would silently corrupt the id, so it is
        // called out by name rather than left to be spotted in the value.
        var duplicated = body.GetProperty("duplicatedHeaders")
            .EnumerateArray().Select(d => d.GetString()!).ToArray();
        Assert.Contains(duplicated, d => d.StartsWith("X-AuthDeep-User-ID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NeverEchoesCredentials()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret, userId: "u1");
        request.Headers.Add("Cookie", "auth.sid=super-secret-session");

        var response = await client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(AuthDeepGatewayFactory.GatewayKey, raw);
        Assert.DoesNotContain(AuthDeepGatewayFactory.ServiceSecret, raw);
        Assert.DoesNotContain("super-secret-session", raw);

        // Redaction is visible, so a hidden header cannot be mistaken for an absent one.
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("<present,", body.GetProperty("headers").GetProperty("X-Gateway-Key").GetString());
    }

    [Fact]
    public async Task WhenGatewaySendsNoUserIdentity_ReportsItAsAbsent()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        // Signed as a service with no end-user headers — the exact shape observed when the
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
