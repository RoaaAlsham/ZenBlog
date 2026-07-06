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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("/api").RequireAuthorization().RegisterEndpoints();

app.Run();
