using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;
    public const long DeletedUserId = -1;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = Context.Set<T>();
    }

    public virtual async Task<bool> Create(T entity)
    {
        await DbSet.AddAsync(entity);
        int entriesWritten = await Context.SaveChangesAsync();
        return entriesWritten > 0;
    }

    public virtual async Task<(bool entriesWritten, long id)> CreateWithId<TEntity>(TEntity entity) where TEntity : class, IHasLongId
    {
        var set = Context.Set<TEntity>();
        await set.AddAsync(entity);
        int entriesWritten = await Context.SaveChangesAsync();
        return (entriesWritten > 0, entity.Id);
    }

    public virtual async Task<TEntity?> GetById<TEntity>(long id) where TEntity : class, IHasLongId
    {
        var set = Context.Set<TEntity>();
        return await set.FirstOrDefaultAsync(entity => entity.Id == id);
    }

    public virtual async Task<List<T>> GetAll() => await DbSet.ToListAsync();

    public virtual async Task<bool> Exists(Expression<Func<T, bool>> predicate) => await DbSet.AnyAsync(predicate);

    public virtual async Task<int> Count() => await DbSet.CountAsync();

    public virtual async Task<bool> Update(T entity)
    {
        DbSet.Update(entity);
        int entriesWritten = await Context.SaveChangesAsync();
        return entriesWritten > 0;
    }

    public virtual async Task<bool> Delete(T entity)
    {
        DbSet.Remove(entity);
        int result = await Context.SaveChangesAsync();
        return result > 0;
    }
}
