using System.Linq.Expressions;

namespace MobyPark.Models.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<bool> Create(T entity);
    Task<(bool entriesWritten, long id)> CreateWithId<TEntity>(TEntity entity) where TEntity : class, IHasLongId;
    Task<TEntity?> GetById<TEntity>(long id) where TEntity : class, IHasLongId;
    Task<List<T>> GetAll();
    Task<bool> Exists(Expression<Func<T, bool>> predicate);
    Task<int> Count();
    Task<bool> Update<TEdit>(T entity, TEdit edit) where TEdit : class, ICanBeEdited;
    Task<bool> Delete(T entity);
    
    
    //CRUD
    void Add(T entity);
    void Update(T entity);
    void Deletee(T entity);
    int SaveChanges();
    
    IEnumerable<T> ReadAll();
    T? FindById(Object id);
    IEnumerable<T> GetBy(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
    
    //Asyncs
    Task<T?> FindByIdAsync(object id);
    Task<List<T>> ReadAllAsync();
    Task<List<T>> GetByAsync(Expression<Func<T, bool>> predicate);
    Task<int> SaveChangesAsync();
    
    
}
