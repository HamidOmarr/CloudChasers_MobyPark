using MobyPark.Services;
using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

public abstract class Repository<T> : IRepository<T> where T : class
{
    protected abstract string TableName { get; }
    protected abstract T MapFromReader(NpgsqlDataReader reader);
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

    public virtual Task<bool> Create(T item) => Create(item, true);
    public virtual async Task<bool> Create(T item, bool returnId)
    {
        var parameters = GetParameters(item);
        parameters.Remove("@id");

        var query = BuildInsertQuery(parameters.Keys.ToList(), returnId);

        if (returnId)
        {
            try
            {
                await Connection.ExecuteScalar(query, parameters);
                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "42703") // Catch column "id" does not exist
            {
                await Connection.ExecuteNonQuery(query, parameters);
                return true;
            }
        }

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

    private string BuildInsertQuery(List<string> keys, bool returnId = true)
    {
        var columns = string.Join(", ", keys.Select(key => key.TrimStart('@')));
        var values = string.Join(", ", keys);
        var query = $"INSERT INTO {TableName} ({columns}) VALUES ({values});";

        if (returnId)
            query += " RETURNING id";

        return query;
    }

    private string BuildUpdateQuery(List<string> keys)
    {
        var assignments = keys
            .Where(key => key != "@id") // Exclude @id from the SET clause
            .Select(key => $"{key.TrimStart('@')} = {key}");
        var setClause = string.Join(", ", assignments);

        return $"UPDATE {TableName} SET {setClause} WHERE id = @id";
    }
}
