using Microsoft.Data.Sqlite;

namespace MobyPark.Services.DatabaseConnection;

class DatabaseConnection : IDatabaseConnection, IAsyncDisposable
{
    private SqliteConnection? _sqliteConnection;

    public DatabaseConnection(IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        OpenConnection(connectionString);
    }

    private void OpenConnection(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("No connection string configured for the database.");

        var builder = new SqliteConnectionStringBuilder(connectionString);
        string dataSource = builder.DataSource;

        if (!Path.IsPathRooted(dataSource))
        {
            string basePath = AppContext.BaseDirectory;
            dataSource = Path.Combine(basePath, dataSource);
            builder.DataSource = dataSource;
        }

        _sqliteConnection = new SqliteConnection(builder.ToString());
        _sqliteConnection.Open();
    }

    public async Task<int> ExecuteQuery(string query)
    {
        await using var command = new SqliteCommand(query, _sqliteConnection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<SqliteDataReader> ExecuteQuery(string query, Dictionary<string, object>? parameters)
    {
        await using var command = new SqliteCommand(query, _sqliteConnection);
        if (parameters != null)
        {
            foreach (KeyValuePair<string, object> parameter in parameters)
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        SqliteDataReader reader = await command.ExecuteReaderAsync();
        return reader;
    }

    public async Task<int> ExecuteNonQuery(string query, Dictionary<string, object> parameters)
    {
        await using var command = new SqliteCommand(query, _sqliteConnection);
        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<(int rowsAffected, object scalar)> ExecuteNonQueryWithScalar(string query, Dictionary<string, object> parameters)
    {
        await using var command = new SqliteCommand(query, _sqliteConnection);
        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT last_insert_rowid();";
        var scalar = await command.ExecuteScalarAsync();
        return (1, scalar ?? 0); // Return 1 for rows affected as per original, and the scalar value
    }

    public async Task<object?> ExecuteScalar(string query, Dictionary<string, object>? parameters)
    {
        await using var command = new SqliteCommand(query, _sqliteConnection);
        if (parameters == null) return await command.ExecuteScalarAsync();
        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        return await command.ExecuteScalarAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqliteConnection != null)
        {
            await _sqliteConnection.DisposeAsync();
            _sqliteConnection = null!;
        }
    }
}