using MobyPark.Services;
using MobyPark.Services.DatabaseConnection;
using Microsoft.Data.Sqlite;

public abstract class Repository<T> : IRepository<T> where T : class
{
    protected abstract string TableName { get; }
    protected abstract T MapFromReader(SqliteDataReader reader);
    protected abstract Dictionary<string, object> GetParameters(T item);
    protected readonly IDatabaseConnection Connection;

    protected Repository(IDatabaseConnection connection)
    {
        Connection = connection;
    }

    public virtual async Task<T?> GetById(int id)
    {
        var parameters = new Dictionary<string, object> { { "@id", id } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE id = @id", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public virtual async Task<List<T>> GetAll()
    {
        var result = new List<T>();
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName}", null);

        while (await reader.ReadAsync())
            result.Add(MapFromReader(reader));

        return result;
    }

    public virtual async Task<int> Count()
    {
        object? result = await Connection.ExecuteScalar($"SELECT COUNT(*) FROM {TableName}", null);
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public virtual async Task<bool> Create(T item)
    {
        var parameters = GetParameters(item);
        parameters.Remove("@id");

        var query = BuildInsertQuery(parameters.Keys.ToList());

        int rowsAffected = await Connection.ExecuteNonQuery(query, parameters);
        return rowsAffected > 0;
    }

    public virtual async Task<(bool success, int id)> CreateWithId(T item)
    {
        var parameters = GetParameters(item);
        parameters.Remove("@id");

        var query = BuildInsertQuery(parameters.Keys.ToList());

        var (rowsAffected, scalar) = await Connection.ExecuteNonQueryWithScalar(query, parameters);
        var id = Convert.ToInt32(scalar);

        return (rowsAffected > 0, id);
    }

    public virtual async Task<bool> Update(T item)
    {
        var parameters = GetParameters(item);

        if (!parameters.ContainsKey("@id"))
            throw new ArgumentException("Item must have an '@id' property for update.");

        var query = BuildUpdateQuery(parameters.Keys.ToList());

        int rowsAffected = await Connection.ExecuteNonQuery(query, parameters);
        return rowsAffected > 0;
    }

    public virtual async Task<bool> Delete(int id)
    {
        var parameters = new Dictionary<string, object> { { "@id", id } };
        int rowsAffected = await Connection.ExecuteNonQuery($"DELETE FROM {TableName} WHERE id = @id", parameters);
        return rowsAffected > 0;
    }

    private string BuildInsertQuery(List<string> keys)
    {
        var columns = string.Join(", ", keys.Select(key => key.TrimStart('@')));
        var values = string.Join(", ", keys);
        return $"INSERT INTO {TableName} ({columns}) VALUES ({values}); SELECT last_insert_rowid();";
    }

    private string BuildUpdateQuery(List<string> keys)
    {
        string columnNames = string.Join(", ", keys.Select(key => key.TrimStart('@')));
        string parameters = string.Join(", ", keys);
        return $"UPDATE {TableName} SET ({columnNames}) = ({parameters}) WHERE id = @id";
    }
}
