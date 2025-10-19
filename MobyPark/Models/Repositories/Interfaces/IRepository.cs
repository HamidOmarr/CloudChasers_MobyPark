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
    Task<bool> Update(T entity);
    Task<bool> Delete(T entity);
}
