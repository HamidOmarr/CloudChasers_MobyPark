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

    public virtual async Task<bool> Update<TEdit>(T entity, TEdit edit) where TEdit : class, ICanBeEdited
    {
        var entry = Context.Entry(entity);
        var editProperties = typeof(TEdit).GetProperties();

        foreach (var property in editProperties)
        {
            var newValue = property.GetValue(edit);
            if (newValue is null) continue;
            var entityProperty = entry.Property(property.Name);
            entityProperty.CurrentValue = newValue;
        }

        int entriesWritten = await Context.SaveChangesAsync();
        return entriesWritten > 0;
    }

    public virtual async Task<bool> Delete(T entity)
    {
        DbSet.Remove(entity);
        int result = await Context.SaveChangesAsync();
        return result > 0;
    }
    
    
    
    //Methods from the class
    
    //Add
    public void Add(T entity)
    {
        if (entity == null) throw new ArgumentNullException("Entity is null");
        DbSet.Add(entity);
    }
    
    //Update
    public void Update(T entity) => DbSet.Update(entity);

    //Delete
    public void Deletee(T entity)
    {
        if (entity == null) throw new ArgumentNullException("Entity is null");
        DbSet.Remove(entity);
    }

    //Save
    public int SaveChanges() => Context.SaveChanges();

    public IEnumerable<T> ReadAll() => DbSet.ToList();
    public T? FindById(object id) => DbSet.Find(id);

    public IEnumerable<T> GetBy(Expression<Func<T, bool>> predicate) => DbSet.Where(predicate);
    public IQueryable<T> Query() => DbSet.AsQueryable();

    public Task<T?> FindByIdAsync(object id) => DbSet.FindAsync(id).AsTask();
    public Task<List<T>> ReadAllAsync() => DbSet.ToListAsync();
    public Task<List<T>> GetByAsync(Expression<Func<T, bool>> predicate) => DbSet.Where(predicate).ToListAsync();
    public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}
