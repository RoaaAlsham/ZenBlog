using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.API.CustomMiddlewares;

namespace ZenBlog.API.IntegrationTests;

/// <summary>
/// Test host with the AuthDeep gateway credentials supplied, which is what switches the
/// verification middleware on under the Testing environment. Every other test factory
/// leaves the keys unset, so the rest of the suite is unaffected.
/// </summary>
public class AuthDeepGatewayFactory : ZenBlogApiFactory
{
    public const string GatewayKey = "gwk_integration_test_gateway_key";
    public const string ServiceSecret = "ssk_integration_test_service_secret_value";

    /// <summary>Identity stashed by the middleware on the most recent verified request.</summary>
    public AuthDeepIdentity? LastIdentity => _capture.Value;

    private readonly IdentityCapture _capture = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AuthDeepGatewayOptions.GatewayKeyKey] = GatewayKey,
                [AuthDeepGatewayOptions.ServiceSecretKey] = ServiceSecret
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(_capture);
            services.AddSingleton<IStartupFilter, IdentityCaptureStartupFilter>();
        });
    }

    public sealed class IdentityCapture
    {
        public AuthDeepIdentity? Value { get; set; }
    }

    /// <summary>
    /// Wraps the application pipeline so the identity can be read out of HttpContext.Items
    /// on the way back out — HttpContext is gone by the time the test sees the response.
    /// </summary>
    private sealed class IdentityCaptureStartupFilter(IdentityCapture capture) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                capture.Value = null;
                await nextMiddleware(context);

                if (context.Items.TryGetValue(AuthDeepGatewayMiddleware.IdentityItemKey, out var stashed)
                    && stashed is AuthDeepIdentity identity)
                {
                    capture.Value = identity;
                }
            });

            next(app);
        };
    }
}
