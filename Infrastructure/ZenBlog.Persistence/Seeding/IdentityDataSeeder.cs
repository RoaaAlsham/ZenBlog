using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Options;

namespace ZenBlog.Persistence.Seeding;

public static class IdentityDataSeeder
{
    public const string AdminRoleName = "Admin";

    /// <summary>
    /// Ensures the Admin role and configured bootstrap admin user exist.
    /// Idempotent: does not reset the password if the user already exists.
    /// Callers decide when to invoke this (e.g. Program skips Testing; tests call explicitly).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentityDataSeeder).FullName!);

        if (!options.Enabled)
        {
            logger.LogDebug("AdminSeed is disabled; skipping identity seed.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email))
        {
            throw new InvalidOperationException(
                "AdminSeed is enabled but AdminSeed:Email is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "AdminSeed is enabled but AdminSeed:Password is missing or empty. " +
                "Set it via User Secrets or environment variables.");
        }

        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        await EnsureAdminRoleAsync(roleManager, logger);
        var user = await EnsureAdminUserAsync(userManager, options, logger);
        await EnsureUserInAdminRoleAsync(userManager, user, logger);

        logger.LogInformation(
            "Admin identity seed completed for {Email}.",
            options.Email);
    }

    private static async Task EnsureAdminRoleAsync(
        RoleManager<AppRole> roleManager,
        ILogger logger)
    {
        if (await roleManager.RoleExistsAsync(AdminRoleName))
        {
            return;
        }

        var createRole = await roleManager.CreateAsync(new AppRole
        {
            Id = Guid.NewGuid().ToString(),
            Name = AdminRoleName
        });

        if (createRole.Succeeded)
        {
            logger.LogInformation("Created Identity role '{Role}'.", AdminRoleName);
            return;
        }

        // Concurrent startup may have created the role between Exists and Create.
        if (await roleManager.RoleExistsAsync(AdminRoleName))
        {
            logger.LogInformation(
                "Admin role already exists after concurrent create attempt.");
            return;
        }

        var errors = string.Join(", ", createRole.Errors.Select(e => e.Description));
        throw new InvalidOperationException(
            $"Failed to create role '{AdminRoleName}': {errors}");
    }

    private static async Task<AppUser> EnsureAdminUserAsync(
        UserManager<AppUser> userManager,
        AdminSeedOptions options,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(options.Email);
        if (existing is not null)
        {
            logger.LogInformation(
                "Bootstrap admin {Email} already exists; password left unchanged.",
                options.Email);
            return existing;
        }

        var username = string.IsNullOrWhiteSpace(options.Username)
            ? options.Email.Split('@')[0]
            : options.Username;

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = options.Email,
            UserName = username,
            FirstName = string.IsNullOrWhiteSpace(options.FirstName) ? "Site" : options.FirstName,
            LastName = string.IsNullOrWhiteSpace(options.LastName) ? "Admin" : options.LastName
        };

        var createUser = await userManager.CreateAsync(user, options.Password);
        if (createUser.Succeeded)
        {
            logger.LogInformation("Created bootstrap admin {Email}.", options.Email);
            return user;
        }

        // Concurrent startup may have created the same email between Find and Create.
        var raced = await userManager.FindByEmailAsync(options.Email);
        if (raced is not null)
        {
            logger.LogInformation(
                "Bootstrap admin {Email} already exists after concurrent create attempt; password left unchanged.",
                options.Email);
            return raced;
        }

        var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
        throw new InvalidOperationException(
            $"Failed to create bootstrap admin '{options.Email}': {errors}");
    }

    private static async Task EnsureUserInAdminRoleAsync(
        UserManager<AppUser> userManager,
        AppUser user,
        ILogger logger)
    {
        if (await userManager.IsInRoleAsync(user, AdminRoleName))
        {
            return;
        }

        var addToRole = await userManager.AddToRoleAsync(user, AdminRoleName);
        if (addToRole.Succeeded)
        {
            logger.LogInformation(
                "Assigned '{Role}' role to {Email}.",
                AdminRoleName,
                user.Email);
            return;
        }

        if (await userManager.IsInRoleAsync(user, AdminRoleName))
        {
            return;
        }

        var errors = string.Join(", ", addToRole.Errors.Select(e => e.Description));
        throw new InvalidOperationException(
            $"Failed to assign role '{AdminRoleName}' to '{user.Email}': {errors}");
    }
}
