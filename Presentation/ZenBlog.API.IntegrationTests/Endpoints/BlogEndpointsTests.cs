using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Application.Features.Blogs.Results;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class BlogEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetBlogById_IncludesAuthorUsername_WithoutEmail()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-author-link@example.com", "Password123!");
        _client.UseGatewayUser(owner.Id);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Author Link Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Author linked blog");

        _client.UseGatewayUser(null);
        var response = await _client.GetAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("@example.com", body, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("user", out var userEl));
        Assert.False(userEl.TryGetProperty("email", out _));
        Assert.True(userEl.TryGetProperty("username", out var usernameEl));
        Assert.False(string.IsNullOrWhiteSpace(usernameEl.GetString()));

        var blog = JsonSerializer.Deserialize<GetBlogsQueryResult>(body, JsonOptions);
        Assert.NotNull(blog);
        Assert.NotNull(blog.User);
        Assert.Equal(owner.Id, blog.User.Id);
        Assert.False(string.IsNullOrWhiteSpace(blog.User.Username));
    }

    [Fact]
    public async Task DeleteBlog_WithoutAuth_ReturnsUnauthorized()
    {
        _client.UseGatewayUser(null);

        var response = await _client.DeleteAsync($"/api/blogs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_AsNonOwnerNonAdmin_ReturnsForbidden()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-owner@example.com", "Password123!");
        _client.UseGatewayUser(owner.Id);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Owner Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Owner blog");

        var otherUser = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-intruder@example.com", "Password123!");
        _client.UseGatewayUser(otherUser.Id);

        var response = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_AsOwner_ReturnsOk()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-owner-delete@example.com", "Password123!");
        _client.UseGatewayUser(owner.Id);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Delete Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Deletable blog");

        var response = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_AsAdminNonOwner_ReturnsOk()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-owner-admin-case@example.com", "Password123!");
        _client.UseGatewayUser(owner.Id);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Admin Delete Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Admin deletable blog");

        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-admin@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseGatewayUser(admin.Id);

        var response = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
