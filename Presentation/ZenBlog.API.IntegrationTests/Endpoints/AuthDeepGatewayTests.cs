using System.Net;
using System.Text;
using ZenBlog.API.IntegrationTests.Helpers;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class AuthDeepGatewayTests(AuthDeepGatewayFactory factory) : IClassFixture<AuthDeepGatewayFactory>
{
    private readonly AuthDeepGatewayFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private const string GatewayRejection = "did not come through the AuthDeep gateway";

    /// <summary>
    /// Golden vector produced independently with openssl:
    ///   printf 'GET\n/api/users/me\n1786115441\n&lt;sha256 of empty&gt;' | openssl dgst -sha256 -hmac 'ssk_test_secret'
    /// Pins the canonicalisation — a stray trailing newline or CRLF separator changes this hash.
    /// </summary>
    [Fact]
    public void SignatureHeader_MatchesIndependentlyComputedVector()
    {
        var header = AuthDeepSigner.BuildSignatureHeader(
            serviceSecret: "ssk_test_secret",
            method: "GET",
            path: "/api/users/me",
            timestamp: 1786115441L);

        Assert.Equal(
            "t=1786115441,v1=8a614c259dcc8d6ac6816d00cab49d7d0669464858d2bbbe994df0941d23db6a",
            header);
    }

    /// <summary>
    /// The gateway strips a trailing slash before signing, so both spellings of the same
    /// route must produce one payload — GET /api/users/ is mapped with the trailing slash.
    /// </summary>
    [Fact]
    public void SignedPath_IgnoresTrailingSlash()
    {
        var withSlash = AuthDeepSigner.BuildSignatureHeader("ssk_test_secret", "GET", "/api/users/", 1786115441L);
        var withoutSlash = AuthDeepSigner.BuildSignatureHeader("ssk_test_secret", "GET", "/api/users", 1786115441L);

        Assert.Equal(withoutSlash, withSlash);
    }

    [Fact]
    public async Task TrailingSlashRoute_WithValidSignature_PassesThrough()
    {
        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "authdeep-trailing-slash@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");

        // The role travels in the signed request, as the gateway would inject it.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                userId: admin.Id, roles: "Admin");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidSignature_WithConsistentGatewayTimestampHeader_PassesThrough()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "authdeep-ts-ok@example.com", "Password123!");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                timestamp: timestamp, userId: user.Id);
        request.Headers.Add("X-Gateway-Timestamp", timestamp.ToString());
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", user.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidSignature_WithContradictoryGatewayTimestampHeader_ReturnsUnauthorized()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                timestamp: timestamp);
        request.Headers.Add("X-Gateway-Timestamp", (timestamp - 90).ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithNoGatewayHeaders_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithForeignGatewayKey_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            .Sign("gwk_someone_elses_key", AuthDeepGatewayFactory.ServiceSecret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithWrongSignature_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("X-Gateway-Key", AuthDeepGatewayFactory.GatewayKey);
        request.Headers.Add("X-Gateway-Signature",
            $"t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()},v1={new string('a', 64)}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithMalformedSignatureHeader_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("X-Gateway-Key", AuthDeepGatewayFactory.GatewayKey);
        request.Headers.Add("X-Gateway-Signature", "not-a-signature");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithExpiredTimestamp_ReturnsUnauthorized()
    {
        // Correctly signed, but 10 minutes old — outside the 300s replay window.
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                timestamp: staleTimestamp);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithTamperedBody_ReturnsUnauthorized()
    {
        // Signature computed over one body, a different body actually sent.
        var signedBody = Encoding.UTF8.GetBytes("""{"email":"a@example.com","password":"Password123!"}""");
        var sentBody = Encoding.UTF8.GetBytes("""{"email":"attacker@example.com","password":"Password123!"}""");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new ByteArrayContent(sentBody)
        };
        request.Content.Headers.Add("Content-Type", "application/json");
        request.Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret, body: signedBody);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedRoute_WithValidSignature_PassesThroughAndExposesIdentity()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "authdeep-valid@example.com", "Password123!");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                userId: user.Id, roles: "Admin,Editor");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", user.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var identity = _factory.LastIdentity;
        Assert.NotNull(identity);
        Assert.Equal(user.Id, identity.UserId);
        Assert.Equal($"{user.Id}@authdeep.test", identity.Email);
        Assert.Equal(["Admin", "Editor"], identity.Roles);
        Assert.NotNull(identity.RequestId);
    }

    [Fact]
    public async Task ProtectedRoute_WithSignedBody_PassesThroughAndBodyStillBinds()
    {
        // Proves the body survives hashing: the login handler must actually read the
        // credentials, so a rewind failure would surface as a binding error, not a 401.
        var body = Encoding.UTF8.GetBytes("""{"email":"no-such-user@example.com","password":"Password123!"}""");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new ByteArrayContent(body)
        };
        request.Content.Headers.Add("Content-Type", "application/json");
        request.Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret, body: body);

        var response = await _client.SendAsync(request);

        // Whatever the handler decides about the credentials, it must NOT be the gateway rejection.
        Assert.DoesNotContain(GatewayRejection, await response.Content.ReadAsStringAsync());
        Assert.NotNull(_factory.LastIdentity);
    }

    [Fact]
    public async Task SignedRequest_WithQueryString_VerifiesBecauseQueryIsNotSigned()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "authdeep-query@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, user.Id, "Admin");

        // AuthDeepSigner signs only the path; the query must not be part of the payload.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/monitoring/activities?page=2&pageSize=5")
            .Sign(AuthDeepGatewayFactory.GatewayKey, AuthDeepGatewayFactory.ServiceSecret,
                userId: user.Id, roles: "Admin");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicBlogRead_WithNoGatewayHeaders_StillSucceeds()
    {
        var response = await _client.GetAsync("/api/blogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/categories")]
    [InlineData("/api/settings")]
    [InlineData("/health")]
    public async Task PublicRoutes_WithNoGatewayHeaders_AreNotRejectedByTheGateway(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicPrefix_DoesNotLeakToSimilarlyNamedPath()
    {
        // "/api/blogsecret" must not be treated as the public "/api/blogs" prefix.
        var response = await _client.GetAsync("/api/blogsecret");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PublicReadPrefix_StillProtectsWriteVerbs()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/blogs", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GatewayRejection, await response.Content.ReadAsStringAsync());
    }
}
