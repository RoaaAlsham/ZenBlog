using System.Net;
using System.Net.Http.Json;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Application.Features.Categories.Commands;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class CategoryEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateCategory_AsNonAdmin_ReturnsForbidden()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-create-user@example.com", "Password123!");
        _client.UseBearerToken(user.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
        {
            CategoryName = "Should Fail"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsNonAdmin_ReturnsForbidden()
    {
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Update Forbidden Category");
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-update-user@example.com", "Password123!");
        _client.UseBearerToken(user.AccessToken);

        var response = await _client.PutAsJsonAsync($"/api/categories/{categoryId}", new UpdateCategoryCommand(
            categoryId,
            "Hacked Name"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

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
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(_client, _factory, "Admin Delete Category");

        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-admin@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseBearerToken(admin.AccessToken);

        var response = await _client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_ReturnsCreated()
    {
        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory, _client, "category-create-admin@example.com", "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseBearerToken(admin.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
        {
            CategoryName = "Admin Created Category"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
