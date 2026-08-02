

using System.Linq.Expressions;
using ZenBlog.Domain.Entities.Common;

namespace ZenBlog.Application.Contracts.Persistence
{
    public interface IRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct);

        IQueryable<TEntity> GetQuery();
        Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct);
        Task<List<TEntity>> GetAllAsync(CancellationToken ct);

        Task CreateAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);

        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Bulk-deletes entities matching the filter without loading them into memory.
        /// Returns the number of rows deleted.
        /// </summary>
        Task<int> DeleteWhereAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default);

        Task<List<TEntity>> GetAllWithIncludesAsync(
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes);

        Task<List<TEntity>> GetAllWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity?> GetSingleWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Loads entities with EF include paths (supports nested paths like "Replies.User").
        /// </summary>
        Task<List<TEntity>> GetAllWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default,
            params string[] includePaths);

        /// <summary>
        /// Loads a single entity with EF include paths (supports nested paths like "Replies.User").
        /// </summary>
        Task<TEntity?> GetSingleWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default,
            params string[] includePaths);

        /// <summary>
        /// Paged query with EF include paths. Ordered by CreatedAt descending.
        /// </summary>
        Task<(List<TEntity> Items, int TotalCount)> GetPagedWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            int page,
            int pageSize,
            CancellationToken ct = default,
            params string[] includePaths);

        Task<int> CountAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default);
    }


}
