using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ZenBlog.API.IntegrationTests.Helpers;

namespace ZenBlog.API.IntegrationTests.Endpoints;

/// <summary>
/// The separation the whole integration rests on: a human caller carries an identity, a
/// service key does not — and a service key that *claims* one is not believed.
///
/// These are the assertions behind the "done when" list. They are written against the
/// diagnostics endpoint because it is the only place that reports the parsed identity and
/// the resulting principal side by side, which is exactly the pair that has to stay
/// consistent.
/// </summary>
public class AuthDeepAuthTypeTests
{
    private const string Route = "/api/diagnostics/gateway-identity";

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

    private static async Task<JsonElement> ReportAsync(HttpRequestMessage request, HttpClient client)
    {
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// What the wire actually looks like: the gateway sends no auth-type header at all,
    /// so a person is recognised by the fact that a user id was injected.
    /// </summary>
    [Fact]
    public async Task HumanWithNoDeclaredAuthType_IsInferredFromTheInjectedUserId()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        const string userId = "b71d0c94-2f6a-4ad3-8c15-9e0f7a2b6c38";

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: userId,
                roles: "admin");

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("Human", identity.GetProperty("authType").GetString());
        Assert.Equal(userId, identity.GetProperty("userId").GetString());
        Assert.True(identity.GetProperty("isHuman").GetBoolean());
        Assert.True(body.GetProperty("principalIsAuthenticated").GetBoolean());
        Assert.Contains("Admin", body.GetProperty("principalRoles").EnumerateArray()
            .Select(r => r.GetString()));
    }

    /// <summary>
    /// The realistic spoofing attempt: no auth-type header to give the game away, just a
    /// service key call dressed with user headers. The key id is what betrays it.
    /// </summary>
    [Fact]
    public async Task ApiKeyWithNoDeclaredAuthType_IsInferredFromTheKeyId_AndStripped()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: "spoofed-user",
                roles: "admin,super_admin",
                apiKeyId: "sak_0000test");

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("ApiKey", identity.GetProperty("authType").GetString());
        Assert.Equal(JsonValueKind.Null, identity.GetProperty("userId").ValueKind);
        Assert.Empty(identity.GetProperty("roles").EnumerateArray());
        Assert.False(identity.GetProperty("isHuman").GetBoolean());
        Assert.False(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    /// <summary>
    /// A hop with neither a user id nor a key id is nobody, and must not become somebody.
    /// </summary>
    [Fact]
    public async Task NoIdentityHeaders_IsAnonymous()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("Unknown", identity.GetProperty("authType").GetString());
        Assert.False(identity.GetProperty("isHuman").GetBoolean());
        Assert.False(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    /// <summary>
    /// Tenant is asserted on every forwarded hop, and X-Forwarded-Tenant-Id stands in if
    /// the canonical header is ever absent.
    /// </summary>
    [Fact]
    public async Task ForwardedHeaders_StandInForTheCanonicalOnes()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret);

        request.Headers.Add("X-Forwarded-User-Id", "0d3f8b21-5c7e-4a19-9f26-3b8d1e4c7a05");
        request.Headers.Add("X-Forwarded-Tenant-Id", "7e2a9c14-6b3d-4f80-a5e1-2c9f6b0d3a48");

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("0d3f8b21-5c7e-4a19-9f26-3b8d1e4c7a05", identity.GetProperty("userId").GetString());
        Assert.Equal("7e2a9c14-6b3d-4f80-a5e1-2c9f6b0d3a48", identity.GetProperty("tenantId").GetString());
        Assert.True(identity.GetProperty("isHuman").GetBoolean());
    }

    /// <summary>
    /// A `wat_` caller is a person: identity present, principal authenticated.
    /// Uses an explicitly declared auth type, which the gateway does not currently send
    /// but which is honoured ahead of inference if it ever appears.
    /// </summary>
    [Fact]
    public async Task WebToken_IsTreatedAsHuman()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        const string userId = "6a2c1f30-0e7e-4f4d-9e2a-7c6b5d4a3f21";

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: userId,
                roles: "admin",
                authType: "web_token");

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("WebToken", identity.GetProperty("authType").GetString());
        Assert.Equal(userId, identity.GetProperty("userId").GetString());
        Assert.True(identity.GetProperty("isHuman").GetBoolean());
        Assert.True(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    /// <summary>
    /// A browser session caller is equally a person — same injection, different transport.
    /// </summary>
    [Fact]
    public async Task Session_IsTreatedAsHuman()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: "9d1f4c2b-8a3e-4c5d-b6f7-0e1a2b3c4d5e",
                authType: "session");

        var body = await ReportAsync(request, client);

        Assert.Equal("Session", body.GetProperty("parsedIdentity").GetProperty("authType").GetString());
        Assert.True(body.GetProperty("principalIsAuthenticated").GetBoolean());
    }

    /// <summary>
    /// The spoofing test. A service key presenting user headers must not become a user.
    ///
    /// The real gateway strips these before they ever arrive, so this asserts the second
    /// line of defence: even if one leaked through — a gateway regression, a
    /// misconfigured route, a direct call from inside the network holding the ssk_ — the
    /// service still refuses to read a person into an api_key request.
    /// </summary>
    [Fact]
    public async Task ApiKey_WithSpoofedUserHeaders_CarriesNoUserIdentity()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: "attacker-supplied-user-id",
                roles: "admin,super_admin",
                authType: "api_key",
                apiKeyId: "sak_0000test");

        var body = await ReportAsync(request, client);
        var identity = body.GetProperty("parsedIdentity");

        Assert.Equal("ApiKey", identity.GetProperty("authType").GetString());

        // Every user-shaped field is dropped.
        Assert.Equal(JsonValueKind.Null, identity.GetProperty("userId").ValueKind);
        Assert.Equal(JsonValueKind.Null, identity.GetProperty("email").ValueKind);
        Assert.Empty(identity.GetProperty("roles").EnumerateArray());
        Assert.False(identity.GetProperty("isHuman").GetBoolean());

        // Key metadata is kept — it is legitimately about the caller.
        Assert.Equal("sak_0000test", identity.GetProperty("apiKeyId").GetString());

        // And nothing reaches the authorization layer, so no admin endpoint can be
        // satisfied by presenting a service key with an "admin" role header.
        Assert.False(body.GetProperty("principalIsAuthenticated").GetBoolean());
        Assert.Empty(body.GetProperty("principalRoles").EnumerateArray());
    }

    /// <summary>
    /// An api_key caller must be refused by an endpoint written for a person, rather than
    /// merely arriving without claims.
    /// </summary>
    [Fact]
    public async Task ApiKey_CannotSatisfyAnAdminEndpoint()
    {
        using var factory = new AuthDeepGatewayFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users")
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: "attacker-supplied-user-id",
                roles: "Admin",
                authType: "api_key",
                apiKeyId: "sak_0000test");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// The correlation id moved to X-Gateway-Request-Id; the old name still works so a
    /// gateway mid-rollout does not lose its trace.
    /// </summary>
    [Fact]
    public async Task RequestId_PrefersTheGatewayHeader()
    {
        using var factory = new DiagnosticsEnabledFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route)
            .Sign(
                AuthDeepGatewayFactory.GatewayKey,
                AuthDeepGatewayFactory.ServiceSecret,
                userId: "3c8e5a17-4b2d-4e6f-8a90-1b2c3d4e5f60",
                authType: "web_token");

        request.Headers.Remove("X-Request-Id");
        request.Headers.Add("X-Request-Id", "legacy-id");
        request.Headers.Add("X-Gateway-Request-Id", "gateway-id");

        var body = await ReportAsync(request, client);

        Assert.Equal("gateway-id", body.GetProperty("parsedIdentity").GetProperty("requestId").GetString());
    }
}
