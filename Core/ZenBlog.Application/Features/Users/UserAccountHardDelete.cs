using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Comments;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users;

/// <summary>
/// Hard-deletes a user's blogs and comments (including reply subtrees under their comments)
/// so Identity user deletion is not blocked by Restrict FKs.
/// Also removes tracked Cloudinary cover images for those blogs.
/// </summary>
public static class UserAccountHardDelete
{
    public const string AdminRoleName = "Admin";

    public static async Task PurgeContentAsync(
        string userId,
        IRepository<Comment> commentRepository,
        IRepository<Blog> blogRepository,
        IImageStorageService imageStorage,
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
            if (!string.IsNullOrWhiteSpace(blog.CoverImagePublicId))
            {
                await imageStorage.DeleteAsync(blog.CoverImagePublicId, cancellationToken);
            }

            await blogRepository.DeleteAsync(blog);
        }
    }
}
