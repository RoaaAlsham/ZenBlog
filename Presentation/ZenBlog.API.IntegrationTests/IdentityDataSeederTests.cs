using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.API.IntegrationTests.Helpers;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Seeding;

namespace ZenBlog.API.IntegrationTests;

public class IdentityDataSeederTests : IClassFixture<AdminSeedApiFactory>
{
    private readonly AdminSeedApiFactory _factory;
    private readonly HttpClient _client;

    public IdentityDataSeederTests(AdminSeedApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_IsIdempotent_AndKeepsPassword()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            Assert.True(await roleManager.RoleExistsAsync(IdentityDataSeeder.AdminRoleName));

            var user = await userManager.FindByEmailAsync(AdminSeedApiFactory.SeedEmail);
            Assert.NotNull(user);
            Assert.Equal(AdminSeedApiFactory.SeedUsername, user.UserName);
            Assert.True(await userManager.IsInRoleAsync(user, IdentityDataSeeder.AdminRoleName));
        }

        var login = await ApiTestHelpers.LoginAsync(
            _client,
            AdminSeedApiFactory.SeedEmail,
            AdminSeedApiFactory.SeedPassword);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_DoesNothing()
    {
        await using var factory = new ZenBlogApiFactory();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        Assert.False(await roleManager.RoleExistsAsync(IdentityDataSeeder.AdminRoleName));
        Assert.Null(await userManager.FindByEmailAsync(AdminSeedApiFactory.SeedEmail));
    }
}
