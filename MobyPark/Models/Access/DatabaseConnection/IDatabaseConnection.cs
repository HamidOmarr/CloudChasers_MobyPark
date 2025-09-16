using Microsoft.Data.Sqlite;

namespace MobyPark.Services.DatabaseConnection;

public interface IDatabaseConnection : IAsyncDisposable // Inherit from IDisposable for synchronous Dispose if needed, or replace with IAsyncDisposable
{
    Task<int> ExecuteQuery(string query);
    Task<SqliteDataReader> ExecuteQuery(string query, Dictionary<string, object>? parameters);
    Task<int> ExecuteNonQuery(string query, Dictionary<string, object> parameters);
    Task<(int rowsAffected, object scalar)> ExecuteNonQueryWithScalar(string query, Dictionary<string, object> parameters);
    Task<object?> ExecuteScalar(string query, Dictionary<string, object>? parameters);
}
