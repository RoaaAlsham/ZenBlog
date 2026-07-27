using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Contracts.Identity;

public sealed class IdentityOperationResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static IdentityOperationResult Success() => new() { Succeeded = true };

    public static IdentityOperationResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };

    public static IdentityOperationResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}

public interface IUserAccountService
{
    Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> CreateAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> ChangePasswordAsync(
        AppUser user,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> DeleteAsync(AppUser user, CancellationToken cancellationToken = default);
}
