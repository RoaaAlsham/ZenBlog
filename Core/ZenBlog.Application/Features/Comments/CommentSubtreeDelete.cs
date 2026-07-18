using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments;

/// <summary>
/// Hard-deletes a comment and its reply subtree (children before parents)
/// to satisfy ParentComment Restrict FKs.
/// </summary>
public static class CommentSubtreeDelete
{
    public static async Task DeleteAsync(
        Guid commentId,
        IRepository<Comment> commentRepository,
        CancellationToken cancellationToken)
    {
        var replies = await commentRepository.GetAllWithIncludesAsync(
            c => c.ParentCommentId == commentId,
            cancellationToken);

        foreach (var reply in replies)
        {
            await DeleteAsync(reply.Id, commentRepository, cancellationToken);
        }

        var comment = await commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment is not null)
        {
            await commentRepository.DeleteAsync(comment);
        }
    }
}
