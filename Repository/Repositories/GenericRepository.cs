using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;
using System.Linq.Expressions;

namespace Repository.Repositories
{
    public class GenericRepository<TEntity> where  TEntity : class
    {   
        private DbContext _context;
        private DbSet<TEntity> _dbSet;
        
        public GenericRepository(DbContext context)
        {
            this._context = context;
            this._dbSet = _context.Set<TEntity>();
        }
        
        public virtual List<TEntity> getAll()
        {
            return _dbSet.ToList();
        }

        public virtual TEntity? FindOne(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.FirstOrDefault(predicate);
        }
        
        public virtual TEntity? getById(int id)
        {
            return _dbSet.Find(id);
            
        }
        
        public virtual void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null) _dbSet.Remove(entity);
        }

        public virtual bool Any(Expression<Func<TEntity, bool>> predicate) => _dbSet.Any(predicate);

        public virtual void RemoveRelated<TChild>(IEnumerable<TChild> entities) where TChild : class
            => _context.RemoveRange(entities.ToList());

        public virtual void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }
        
        public virtual void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }
        
        public virtual void Insert(TEntity entity)
        {
            _dbSet.Add(entity);
            
        }

        public virtual void Persist(TEntity entity)
        {
            _context.SaveChanges();
        }
    }    
}
