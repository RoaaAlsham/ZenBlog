using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZenBlog.Persistence.Context;

namespace ZenBlog.API.IntegrationTests;

public class ZenBlogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = _databaseName,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=test;Password=test",
                ["Cors:AllowedOrigins"] = "http://localhost",
                ["JwtSettings:Secret"] = "integration-test-secret-key-32chars!",
                ["JwtSettings:Issuer"] = "ZenBlogAPI",
                ["JwtSettings:Audience"] = "ZenBlogClient",
                ["JwtSettings:ExpiryMinutes"] = "60",
                ["CloudinarySettings:CloudName"] = "demo",
                ["CloudinarySettings:ApiKey"] = "test-key",
                ["CloudinarySettings:ApiSecret"] = "test-secret"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
    }
}
