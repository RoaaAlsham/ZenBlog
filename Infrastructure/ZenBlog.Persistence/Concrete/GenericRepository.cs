
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities.Common;
using ZenBlog.Persistence.Context;

namespace ZenBlog.Persistence.Concrete
{
    public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }
        public async Task CreateAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync([id], ct);
        }

        public async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(ct);
        }
    

        public IQueryable<TEntity> GetQuery()
        {
            return _dbSet.AsNoTracking().AsQueryable();
        }

        public async Task<TEntity?> GetSingleAsync(
       Expression<Func<TEntity, bool>> filter,
       CancellationToken ct = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(filter, ct);
        }

        public Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<List<TEntity>> GetAllWithIncludesAsync(
      CancellationToken ct = default,
      params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
                query = query.Include(include);  
            return await query.ToListAsync(ct);
        }
        public async Task<List<TEntity>> GetAllWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.Where(filter).ToListAsync(ct);  
        }
        public async Task<TEntity?> GetSingleWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.FirstOrDefaultAsync(filter, ct);
        }

        public async Task<List<TEntity>> GetAllWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default,
            params string[] includePaths)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var path in includePaths)
                query = query.Include(path);
            return await query.Where(filter).ToListAsync(ct);
        }

        public async Task<TEntity?> GetSingleWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken ct = default,
            params string[] includePaths)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var path in includePaths)
                query = query.Include(path);
            return await query.FirstOrDefaultAsync(filter, ct);
        }

        public async Task<(List<TEntity> Items, int TotalCount)> GetPagedWithIncludePathsAsync(
            Expression<Func<TEntity, bool>> filter,
            int page,
            int pageSize,
            CancellationToken ct = default,
            params string[] includePaths)
        {
            IQueryable<TEntity> filtered = _dbSet.AsNoTracking().Where(filter);
            var totalCount = await filtered.CountAsync(ct);

            IQueryable<TEntity> query = filtered.OrderByDescending(e => e.CreatedAt);
            foreach (var path in includePaths)
                query = query.Include(path);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}
