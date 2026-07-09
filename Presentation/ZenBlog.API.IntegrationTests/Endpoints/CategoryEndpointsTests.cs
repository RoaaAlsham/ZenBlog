using System.Net;
using ZenBlog.API.IntegrationTests.Helpers;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class CategoryEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeleteCategory_AsNonAdmin_ReturnsForbidden()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-user@example.com", "Password123!");
        _client.UseBearerToken(user.AccessToken);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Protected Category");

        var response = await _client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_ReturnsNoContent()
    {
        var creator = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-creator@example.com", "Password123!");
        _client.UseBearerToken(creator.AccessToken);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Admin Delete Category");

        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-admin@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseBearerToken(admin.AccessToken);

        var response = await _client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
