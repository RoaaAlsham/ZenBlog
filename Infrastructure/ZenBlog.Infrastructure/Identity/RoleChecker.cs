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
}
