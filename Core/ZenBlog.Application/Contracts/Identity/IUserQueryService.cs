using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Contracts.Identity;

public interface IUserQueryService
{
    Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
}
