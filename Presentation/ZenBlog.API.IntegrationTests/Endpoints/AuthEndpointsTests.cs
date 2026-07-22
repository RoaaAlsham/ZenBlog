using System.Net;
using System.Net.Http.Json;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Application.Features.Auth.Commands;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class AuthEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        await ApiTestHelpers.RegisterUserAsync(_factory, _client, "auth-user@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand
        {
            Email = "auth-user@example.com",
            Password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        const string email = "login-success@example.com";
        const string password = "Password123!";
        await ApiTestHelpers.RegisterUserAsync(_factory, _client, email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("refreshToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("username", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand
        {
            Email = string.Empty,
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        _client.UseBearerToken(null);

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = "not-a-real-refresh-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = string.Empty
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_ReturnsSuccess()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new LogoutCommand
        {
            RefreshToken = "not-a-real-refresh-token"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
