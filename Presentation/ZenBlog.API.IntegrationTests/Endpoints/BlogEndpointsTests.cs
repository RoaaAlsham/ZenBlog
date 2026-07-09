using System.Net;
using ZenBlog.API.IntegrationTests.Helpers;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class BlogEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeleteBlog_WithoutAuth_ReturnsUnauthorized()
    {
        _client.UseBearerToken(null);

        var response = await _client.DeleteAsync($"/api/blogs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_AsNonOwnerNonAdmin_ReturnsForbidden()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-owner@example.com", "Password123!");
        _client.UseBearerToken(owner.AccessToken);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Owner Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Owner blog");

        var otherUser = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-intruder@example.com", "Password123!");
        _client.UseBearerToken(otherUser.AccessToken);

        var response = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_AsOwner_ReturnsOk()
    {
        var owner = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-owner-delete@example.com", "Password123!");
        _client.UseBearerToken(owner.AccessToken);
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
        _client.UseBearerToken(owner.AccessToken);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Admin Delete Category");
        var blogId = await ApiTestHelpers.CreateBlogAsync(_client, categoryId, "Admin deletable blog");

        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "blog-admin@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseBearerToken(admin.AccessToken);

        var response = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
