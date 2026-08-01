using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Monitoring.Results;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Context;

namespace ZenBlog.API.IntegrationTests.Endpoints;

public class MonitoringEndpointsTests(ZenBlogApiFactory factory) : IClassFixture<ZenBlogApiFactory>
{
    private readonly ZenBlogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Overview_Unauthenticated_ReturnsUnauthorized()
    {
        _client.UseBearerToken(null);
        var response = await _client.GetAsync("/api/monitoring/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Overview_NonAdmin_ReturnsForbidden()
    {
        var user = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "monitor-user@example.com",
            "Password123!");
        _client.UseBearerToken(user.AccessToken);

        var response = await _client.GetAsync("/api/monitoring/overview");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Admin_ReturnsOk()
    {
        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "monitor-admin@example.com",
            "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);
        _client.UseBearerToken(admin.AccessToken);

        var response = await _client.GetAsync("/api/monitoring/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var overview = await response.Content.ReadFromJsonAsync<MonitoringOverviewResult>(JsonOptions);
        Assert.NotNull(overview);
    }

    [Fact]
    public async Task Activities_Admin_ReturnsPagedFeed()
    {
        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "monitor-activities@example.com",
            "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            db.ActivityLogs.Add(new ActivityLog
            {
                OccurredAtUtc = now,
                ActorUserId = admin.Id,
                ActorDisplayName = "Test User",
                Action = "Blog.Created",
                EntityType = "Blog",
                EntityId = Guid.NewGuid().ToString(),
                Summary = "Created blog 'Integration'",
                Success = true
            });
            await db.SaveChangesAsync();
        }

        _client.UseBearerToken(admin.AccessToken);
        var response = await _client.GetAsync("/api/monitoring/activities?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<ActivityLogResult>>(JsonOptions);
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 1);
        Assert.Contains(page.Items, i => i.Action == "Blog.Created");
    }

    [Fact]
    public async Task SecurityRequests_Admin_IncludesLoginFailure()
    {
        var admin = await ApiTestHelpers.RegisterAndLoginAsync(
            _factory,
            _client,
            "monitor-security@example.com",
            "Password123!");
        await ApiTestHelpers.AssignRoleAsync(_factory, admin.Id, "Admin");
        admin.AccessToken = await ApiTestHelpers.CreateAccessTokenAsync(_factory, admin.Id);

        _client.UseBearerToken(null);
        var failedLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "monitor-security@example.com",
            password = "WrongPassword!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        _client.UseBearerToken(admin.AccessToken);
        var response = await _client.GetAsync("/api/monitoring/security-requests?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<SecurityRequestLogResult>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Contains(
            page.Items,
            i => i.EventType == SecurityEventType.LoginFailure
                 && i.Path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase));
    }
}
