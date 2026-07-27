using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Identity;

public sealed class RoleChecker(UserManager<AppUser> userManager) : IRoleChecker
{
    public async Task<IReadOnlyList<string>> GetRolesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return [];
        }

        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(userId, cancellationToken);
        return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountUsersInRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        return users.Count;
    }
}
