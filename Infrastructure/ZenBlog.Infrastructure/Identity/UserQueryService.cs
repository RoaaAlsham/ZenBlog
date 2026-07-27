using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Infrastructure.Identity;

public sealed class UserQueryService(UserManager<AppUser> userManager) : IUserQueryService
{
    public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        => userManager.FindByIdAsync(userId);

    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        => userManager.FindByEmailAsync(email);

    public Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        => userManager.FindByNameAsync(userName);

    public Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Materialize without referencing EF Core from this project.
        IReadOnlyList<AppUser> users = userManager.Users.ToList();
        return Task.FromResult(users);
    }
}
