using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Identity;

public sealed class UserAccountService(UserManager<AppUser> userManager) : IUserAccountService
{
    public Task<bool> CheckPasswordAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken = default)
        => userManager.CheckPasswordAsync(user, password);

    public async Task<IdentityOperationResult> CreateAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken = default)
    {
        var result = await userManager.CreateAsync(user, password);
        return Map(result);
    }

    public async Task<IdentityOperationResult> UpdateAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        var result = await userManager.UpdateAsync(user);
        return Map(result);
    }

    public async Task<IdentityOperationResult> ChangePasswordAsync(
        AppUser user,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return Map(result);
    }

    public async Task<IdentityOperationResult> DeleteAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        var result = await userManager.DeleteAsync(user);
        return Map(result);
    }

    private static IdentityOperationResult Map(IdentityResult result)
        => result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(e => e.Description));
}
