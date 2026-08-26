using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class UserEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetMe_Unauthenticated_ReturnsUnauthorized()
    {
        _client.UseGatewayUser(null);
        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsProfile()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "profile-me@example.com",
            "Password123!");
        _client.UseGatewayUser(user.Id);

        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResult>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal(user.Email, profile.Email);
        Assert.Equal("Test", profile.FirstName);
        Assert.Equal("User", profile.LastName);
        Assert.False(string.IsNullOrWhiteSpace(profile.Username));
    }

    [Fact]
    public async Task UpdateMe_UpdatesNames()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "profile-update@example.com",
            "Password123!");
        _client.UseGatewayUser(user.Id);

        var response = await _client.PutAsJsonAsync("/api/users/me", new UpdateProfileCommand
        {
            FirstName = "Updated",
            LastName = "Name",
            ImageUrl = null,
            ImagePublicId = null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResult>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal("Name", profile.LastName);
        Assert.Null(profile.ImageUrl);
    }

    [Fact]
    public async Task ChangePassword_ThenLoginWithNewPassword_Succeeds()
    {
        const string email = "profile-password@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "NewPassword123!";

        var user = await ApiTestHelpers.RegisterAndLoginAsync(_factory, _client, email, oldPassword);
        _client.UseGatewayUser(user.Id);

        var changeResponse = await _client.PutAsJsonAsync("/api/users/me/password", new ChangePasswordCommand
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        _client.UseGatewayUser(null);
        var login = await ApiTestHelpers.LoginAsync(_client, email, newPassword);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.False(string.IsNullOrWhiteSpace(login.Username));
    }

    [Fact]
    public async Task GetByUsername_DoesNotExposeEmail()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "public-author@example.com",
            "Password123!");
        _client.UseGatewayUser(user.Id);

        var me = await _client.GetFromJsonAsync<UserProfileResult>("/api/users/me", JsonOptions);
        Assert.NotNull(me);
        Assert.False(string.IsNullOrWhiteSpace(me.Username));

        _client.UseGatewayUser(null);
        var response = await _client.GetAsync($"/api/users/by-username/{me.Username}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("@example.com", body, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.TryGetProperty("email", out _));

        var publicUser = JsonSerializer.Deserialize<PublicUserResult>(body, JsonOptions);
        Assert.NotNull(publicUser);
        Assert.Equal(me.Username, publicUser.Username);
        Assert.Equal(user.Id, publicUser.Id);
    }

    [Fact]
    public async Task DeleteMe_Unauthenticated_ReturnsUnauthorized()
    {
        _client.UseGatewayUser(null);
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new DeleteMyAccountCommand { CurrentPassword = "Password123!" })
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Unauthenticated_ReturnsUnauthorized()
    {
        _client.UseGatewayUser(null);
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_NonAdmin_ReturnsForbidden()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "delete-nonadmin@example.com",
            "Password123!");
        _client.UseGatewayUser(user.Id);

        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_NonAdmin_ReturnsForbidden()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "list-nonadmin@example.com",
            "Password123!");
        _client.UseGatewayUser(user.Id);

        var response = await _client.GetAsync("/api/users/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_WithCorrectPassword_RemovesAccount()
    {
        const string email = "self-delete@example.com";
        const string password = "Password123!";

        var user = await ApiTestHelpers.RegisterAndLoginAsync(_factory, _client, email, password);
        _client.UseGatewayUser(user.Id);

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new DeleteMyAccountCommand { CurrentPassword = password })
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _client.UseGatewayUser(null);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
}
