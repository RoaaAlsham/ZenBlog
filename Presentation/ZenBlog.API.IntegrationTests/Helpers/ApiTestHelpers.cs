using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.Application.Features.Auth.Commands;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Auth.Results;
using ZenBlog.Application.Features.Blogs.Results;
using ZenBlog.Application.Features.Categories.Commands;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Context;

namespace ZenBlog.API.IntegrationTests.Helpers;

public static class ApiTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<RegisteredUser> RegisterUserAsync(
        ZenBlogApiFactory factory,
        HttpClient client,
        string email,
        string password,
        string firstName = "Test",
        string lastName = "User")
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var username = email.Split('@')[0].Replace('.', '_').Replace('-', '_');
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new RegisteredUser(existing.Id, existing.Email!, password);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = username,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed user: {errors}");
        }

        return new RegisteredUser(user.Id, user.Email!, password);
    }

    public static async Task<LoginResult> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand
        {
            Email = email,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed ({(int)response.StatusCode}): {body}");
        }

        return await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions)
            ?? throw new InvalidOperationException("Login response was empty.");
    }

    public static async Task<string> CreateAccessTokenAsync(ZenBlogApiFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' was not found.");
        var roles = await userManager.GetRolesAsync(user);
        return tokenGenerator.GenerateToken(user, roles, 15).Token;
    }

    public static async Task<RegisteredUser> RegisterAndLoginAsync(
        ZenBlogApiFactory factory,
        HttpClient client,
        string email,
        string password)
    {
        var user = await RegisterUserAsync(factory, client, email, password);
        user.AccessToken = await CreateAccessTokenAsync(factory, user.Id);
        return user;
    }

    public static async Task<RegisteredUser> RegisterAndLoginViaEndpointAsync(
        ZenBlogApiFactory factory,
        HttpClient client,
        string email,
        string password)
    {
        var user = await RegisterUserAsync(factory, client, email, password);
        var login = await LoginAsync(client, email, password);
        user.AccessToken = login.Token;
        return user;
    }

    public static async Task AssignRoleAsync(
        ZenBlogApiFactory factory,
        string userId,
        string roleName)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var createRole = await roleManager.CreateAsync(new AppRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName
            });
            if (!createRole.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role '{roleName}'.");
            }
        }

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' was not found.");
        var addToRole = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRole.Succeeded)
        {
            throw new InvalidOperationException($"Failed to assign role '{roleName}'.");
        }
    }

    public static void UseBearerToken(this HttpClient client, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = null;
            return;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<Guid> CreateCategoryAsync(HttpClient client, ZenBlogApiFactory factory, string categoryName)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
        {
            CategoryName = categoryName
        });
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = db.Categories.Single(c => c.CategoryName == categoryName);
        return category.Id;
    }

    public static async Task<Guid> CreateBlogAsync(
        HttpClient client,
        Guid categoryId,
        string title = "Test blog",
        string? spoofedUserId = null)
    {
        var response = await client.PostAsJsonAsync("/api/blogs", new
        {
            title,
            description = "Test description",
            coverImageUrl = "cover.png",
            blogImageUrl = "blog.png",
            categoryId,
            userId = spoofedUserId ?? "should-be-overwritten"
        });

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreateBlogResult>(JsonOptions)
            ?? throw new InvalidOperationException("Blog creation response was empty.");

        return created.Id;
    }

    public sealed class RegisteredUser(string id, string email, string password)
    {
        public string Id { get; } = id;
        public string Email { get; } = email;
        public string Password { get; } = password;
        public string? AccessToken { get; set; }
    }
}
