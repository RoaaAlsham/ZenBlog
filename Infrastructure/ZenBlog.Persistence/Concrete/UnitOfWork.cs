
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Persistence.Context;

namespace ZenBlog.Persistence.Concrete
{
    internal class UnitOfWork(AppDbContext _context) : IUnitOfWork
    {
        public async Task<bool> SaveChangesAsync()
        {
            // Zero rows changed is still success (e.g. no-op updates); failures throw.
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
