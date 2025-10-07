namespace MobyPark.Services;

public interface IRepository<T>
{
    Task<T?> GetById(int id);
    Task<List<T>> GetAll();
    Task<bool> Create(T item);
    Task<(bool success, int id)> CreateWithId(T item);
    Task<bool> Update(T item);
    Task<bool> Delete(int id);
    Task<int> Count();
}
