using Npgsql;

namespace MobyPark.Models.Access.DatabaseConnection;

internal class DatabaseConnection : IDatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("No connection string configured for the database.");
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        { throw new InvalidOperationException("Could not open PostgreSQL connection", ex); }
    }

    public async Task<int> ExecuteQuery(string query)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        { throw new InvalidOperationException($"Error executing scalar query: {query}", ex); }
    }

    public async Task<NpgsqlDataReader> ExecuteQuery(string query, Dictionary<string, object>? parameters)
    {
        var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        if (parameters is not null)
        {
            foreach (var param in parameters)
                command.Parameters.AddWithValue(param.Key, param.Value);
        }

        try
        { return await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection); }
        catch (Exception ex)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException($"Error executing reader query: {query}", ex);
        }
    }

    public async Task<int> ExecuteNonQuery(string query, Dictionary<string, object> parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        try
        { return await command.ExecuteNonQueryAsync(); }
        catch (Exception ex)
        { throw new InvalidOperationException($"Error executing non-query: {query}", ex); }
    }

    public async Task<(int rowsAffected, object scalar)> ExecuteNonQueryWithScalar(string query, Dictionary<string, object> parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        foreach (var param in parameters)
        { command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value); }

        try
        {
            var scalar = await command.ExecuteScalarAsync();
            int rowsAffected = scalar != null ? 1 : 0;
            return (rowsAffected, scalar ?? 0);
        }
        catch (Exception ex)
        { throw new InvalidOperationException($"Error executing non-query with scalar: {query}", ex); }
    }

    public async Task<object?> ExecuteScalar(string query, Dictionary<string, object>? parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
                command.Parameters.AddWithValue(param.Key, param.Value);
        }

        try
        { return await command.ExecuteScalarAsync(); }
        catch (Exception ex)
        { throw new InvalidOperationException($"Error executing scalar: {query}", ex); }
    }
}
