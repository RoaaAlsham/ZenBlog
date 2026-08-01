using ZenBlog.Application.Contracts.Identity;

namespace ZenBlog.Application.Features.Monitoring;

public static class ActivityActor
{
    public static async Task<(string? UserId, string? DisplayName)> ResolveAsync(
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return (null, null);
        }

        var user = await userQuery.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return (currentUser.UserId, null);
        }

        return (user.Id, $"{user.FirstName} {user.LastName}");
    }
}
