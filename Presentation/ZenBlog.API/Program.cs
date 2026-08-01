using ZenBlog.Persistence.Extentions;
using ZenBlog.Application.Extensions;
using ZenBlog.Infrastructure.Extensions;
using ZenBlog.API.Endpoints.Registrations;
using Scalar.AspNetCore;
using ZenBlog.API.CustomMiddlewares;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Features.Media;
using ZenBlog.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

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

// ---------------------------------------------------------------------------
// Verify Backend CORS Configuration (required for local Next.js testing)
// ---------------------------------------------------------------------------
// The zenblog_client runs on http://localhost:3000 and calls this API at
// https://localhost:7117. Browsers block that cross-origin traffic unless we
// explicitly allow the frontend origin below.
//
// If your Next.js port differs (e.g. 3001), add it to WithOrigins(...).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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

await app.SeedIdentityDataAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<CustomExceptionHandlingMiddleware>();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Enable CORS before auth and endpoint mapping so browser preflight (OPTIONS)
// requests succeed when zenblog_client calls this API from another origin.
app.UseCors("AllowFrontend");

app.UseRateLimiter();

// UseAuthentication MUST run before UseAuthorization: authentication figures out
// WHO is calling (validates the bearer token, populates HttpContext.User);
// authorization then decides WHAT that identity is allowed to do.
// Previously there was no UseAuthentication() call at all, so UseAuthorization()
// had no identity to check against and any [Authorize]/.RequireAuthorization()
// would have silently done nothing.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("/api").RegisterEndpoints();

app.Run();

public partial class Program { }
