using Microsoft.AspNetCore.Authentication;
using ZenBlog.Persistence.Extentions;
using ZenBlog.Application.Extensions;
using ZenBlog.Infrastructure.Extensions;
using ZenBlog.API.Endpoints.Registrations;
using Scalar.AspNetCore;
using ZenBlog.API.CustomMiddlewares;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Features.Media;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// Make JSON property names case-insensitive (Next.js client sends camelCase).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNameCaseInsensitive = true);

// Multipart hard limit slightly above the 5 MB application validation limit.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ImageUploadLimits.MultipartHardLimitBytes;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = ImageUploadLimits.MultipartHardLimitBytes;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust Render (and similar) reverse proxies that terminate TLS.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

if (corsOrigins.Length == 0 && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins is missing or empty. Set a comma-separated list of allowed frontend origins.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Testing host supplies origins via factory config; empty is allowed only there.
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Add services to the container.
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// The end user is authenticated by AuthDeep, not by this service. The gateway
// resolves them — from a browser session or from a `wat_` plus its PoP signature —
// and injects X-AuthDeep-User-* headers, which AuthDeepGatewayMiddleware verifies
// before anything reads them. This scheme turns that into the ClaimsPrincipal that
// .RequireAuthorization() and RequireRole("Admin") check against.
//
// Registered after AddInfrastructureServices so it overrides the default scheme set
// there. The legacy ZenBlog JWT bearer scheme stays registered for the older
// /api/auth endpoints, but it is no longer the default and no endpoint asks for it
// by name, so nothing reaches an authorization check through it.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = AuthDeepGatewayDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = AuthDeepGatewayDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = AuthDeepGatewayDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, AuthDeepGatewayAuthenticationHandler>(
        AuthDeepGatewayDefaults.AuthenticationScheme,
        displayName: null,
        configureOptions: _ => { });

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Resolve the connection string from DI at check time so WebApplicationFactory
// in-memory config (and env vars) are available; eager GetConnectionString here
// runs before the test host finishes composing configuration.
builder.Services.AddHealthChecks()
    .AddNpgSql(sp =>
        sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is missing from configuration."));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var securityLogger = httpContext.RequestServices.GetService<ISecurityRequestLogger>();
        if (securityLogger is not null)
        {
            var sourceIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var host = httpContext.Request.Host.Value ?? "unknown";
            var path = httpContext.Request.Path.Value ?? "/";
            if (httpContext.Request.QueryString.HasValue)
            {
                path += httpContext.Request.QueryString.Value;
            }

            await securityLogger.LogAsync(
                SecurityEventType.RateLimited,
                StatusCodes.Status429TooManyRequests,
                sourceIp,
                host,
                path,
                cancellationToken);
        }

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    };

    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.AddPolicy("login-per-ip", _ => RateLimitPartition.GetNoLimiter("testing"));
        options.AddPolicy("refresh-per-ip", _ => RateLimitPartition.GetNoLimiter("testing"));
        options.AddPolicy("register-per-ip", _ => RateLimitPartition.GetNoLimiter("testing"));
        options.AddPolicy("media-per-ip", _ => RateLimitPartition.GetNoLimiter("testing"));
    }
    else
    {
        options.AddPolicy("login-per-ip", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ipAddress,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
        // Stricter than login: refresh tokens are long-lived secrets and this endpoint
        // is an attractive replay/guessing target, so allow fewer attempts per IP.
        options.AddPolicy("refresh-per-ip", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ipAddress,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
        // Registration spam / account-farming control.
        options.AddPolicy("register-per-ip", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ipAddress,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
        // Authenticated upload flood would burn Cloudinary quota.
        options.AddPolicy("media-per-ip", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ipAddress,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
    }
});

var app = builder.Build();

// AuthDeep gateway credentials. Required everywhere except the Testing host, so a
// misconfigured deployment throws here — before app.Run() — rather than silently
// accepting unsigned traffic; integration tests opt in by supplying the two keys.
// Resolved from DI rather than builder.Configuration for the same reason as the
// health check above: WebApplicationFactory config is not composed until now.
var authDeepOptions = AuthDeepGatewayOptions.FromConfiguration(
    app.Services.GetRequiredService<IConfiguration>(),
    required: !app.Environment.IsEnvironment("Testing"));

if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await app.SeedIdentityDataAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseForwardedHeaders();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseMiddleware<CustomExceptionHandlingMiddleware>();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Enable CORS before auth and endpoint mapping so browser preflight (OPTIONS)
// requests succeed when zenblog_client calls this API from another origin.
app.UseCors("AllowFrontend");

// Verify the AuthDeep gateway signature before anything else looks at the request:
// ahead of the rate limiter, authentication, authorization and endpoint execution, so
// traffic that did not come through the gateway is rejected at the cheapest point.
// Placed after UseCors so browser preflight (OPTIONS), which carries no gateway
// headers, is still answered by the CORS middleware.
if (authDeepOptions is not null)
{
    app.UseWhen(
        AuthDeepProtectedRoutes.RequiresGatewaySignature,
        protectedBranch =>
        {
            protectedBranch.UseMiddleware<AuthDeepGatewayMiddleware>(authDeepOptions);
            // Straight after verification, so a reader AuthDeep knows has a local row
            // before any handler tries to attach content to them.
            protectedBranch.UseMiddleware<AuthDeepUserProvisioningMiddleware>();
        });
}

app.UseRateLimiter();

// UseAuthentication MUST run before UseAuthorization: authentication figures out
// WHO is calling (validates the bearer token, populates HttpContext.User);
// authorization then decides WHAT that identity is allowed to do.
// Previously there was no UseAuthentication() call at all, so UseAuthorization()
// had no identity to check against and any [Authorize]/.RequireAuthorization()
// would have silently done nothing.
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapGroup("/api").RegisterEndpoints();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
