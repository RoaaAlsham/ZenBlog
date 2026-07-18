using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZenBlog.Persistence.Seeding;

namespace ZenBlog.Persistence.Extentions;

public static class IdentitySeedExtensions
{
    /// <summary>
    /// Runs AdminSeed bootstrap and ensures SiteSettings exists.
    /// No-ops in the Testing environment so integration tests are unaffected;
    /// tests that need seeding call seeders directly.
    /// </summary>
    public static async Task SeedIdentityDataAsync(this IHost host)
    {
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = host.Services.CreateScope();
        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
        await SiteSettingsSeeder.SeedAsync(scope.ServiceProvider);
    }
}
