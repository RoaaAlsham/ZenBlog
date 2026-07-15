using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ZenBlog.API.IntegrationTests;

/// <summary>
/// Test host with AdminSeed enabled so IdentityDataSeeder can be exercised explicitly.
/// Automatic Program.cs seeding still no-ops because the environment remains Testing.
/// </summary>
public class AdminSeedApiFactory : ZenBlogApiFactory
{
    public const string SeedEmail = "seed-admin@example.com";
    public const string SeedPassword = "AdminPassword123!";
    public const string SeedUsername = "seedadmin";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSeed:Enabled"] = "true",
                ["AdminSeed:Email"] = SeedEmail,
                ["AdminSeed:Username"] = SeedUsername,
                ["AdminSeed:FirstName"] = "Seed",
                ["AdminSeed:LastName"] = "Admin",
                ["AdminSeed:Password"] = SeedPassword
            });
        });
    }
}
