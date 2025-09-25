using Npgsql;

namespace MobyPark.Models.Access.DatabaseConnection;

public interface IDatabaseConnection
{
    Task<int> ExecuteQuery(string query);
    Task<NpgsqlDataReader> ExecuteQuery(string query, Dictionary<string, object>? parameters);
    Task<int> ExecuteNonQuery(string query, Dictionary<string, object> parameters);
    Task<(int rowsAffected, object scalar)> ExecuteNonQueryWithScalar(string query, Dictionary<string, object> parameters);
    Task<object?> ExecuteScalar(string query, Dictionary<string, object>? parameters);
}
