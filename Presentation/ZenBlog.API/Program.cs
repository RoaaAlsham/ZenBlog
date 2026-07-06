using ZenBlog.Persistence.Extentions;
using ZenBlog.Application.Extensions;
using ZenBlog.API.Endpoints.Registrations;
using Scalar.AspNetCore;
using ZenBlog.API.CustomMiddlewares;

var builder = WebApplication.CreateBuilder(args);
// Make JSON property names case-insensitive
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNameCaseInsensitive = true);

// Add services to the container.
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<CustomExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

// UseAuthentication() must run before UseAuthorization().Swapping the order would mean authorization checks run
// against an empty/anonymous user every time, and every protected endpoint would return 401.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// RequireAuthorization() here applies to every endpoint registered under "/api" by default.
// Endpoints that must be reachable without a token (register, login) opt out individually with
// .AllowAnonymous() in their own endpoint files

app.MapGroup("/api").RequireAuthorization().RegisterEndpoints();

app.Run();
