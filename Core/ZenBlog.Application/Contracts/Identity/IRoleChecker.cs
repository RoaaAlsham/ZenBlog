namespace ZenBlog.Application.Contracts.Identity;

public interface IRoleChecker
{
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken = default);
}
