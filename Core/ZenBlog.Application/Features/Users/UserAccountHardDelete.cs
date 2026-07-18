using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users;

/// <summary>
/// Hard-deletes a user's blogs and comments (including reply subtrees under their comments)
/// so Identity user deletion is not blocked by Restrict FKs.
/// </summary>
public static class UserAccountHardDelete
{
    public const string AdminRoleName = "Admin";

    public static async Task PurgeContentAsync(
        string userId,
        IRepository<Comment> commentRepository,
        IRepository<Blog> blogRepository,
        CancellationToken cancellationToken)
    {
        var userComments = await commentRepository.GetAllWithIncludesAsync(
            c => c.UserId == userId,
            cancellationToken);

        foreach (var comment in userComments)
        {
            await CommentSubtreeDelete.DeleteAsync(comment.Id, commentRepository, cancellationToken);
        }

        var blogs = await blogRepository.GetAllWithIncludesAsync(
            b => b.UserId == userId,
            cancellationToken);

        foreach (var blog in blogs)
        {
            await blogRepository.DeleteAsync(blog);
        }
    }
}
