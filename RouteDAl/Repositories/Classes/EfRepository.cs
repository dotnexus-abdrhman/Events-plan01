using EvenDAL.Repositories.InterFace;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Repositories.Classes
{
    public class EfRepository<TEntity, TId> : IRepository<TEntity, TId> where TEntity : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public EfRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(TId id)
            => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<TEntity>> ListAsync()
     => await _dbSet
         .AsNoTracking()        // ✅ منع تتبّع الكيانات المقروءة
         .ToListAsync();


        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet
                .AsNoTracking()        // ✅ منع تتبّع
                .Where(predicate)
                .ToListAsync();
        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(TEntity entity)
        {
            // ✅ حماية من: The instance of entity type 'User' cannot be tracked...
            _context.ChangeTracker.Clear();   // يفضي تتبّع السياق قبل Attach/Update
            _dbSet.Update(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteByIdAsync(TId id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
