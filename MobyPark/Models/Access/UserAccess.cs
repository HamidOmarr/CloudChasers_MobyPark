using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class UserAccess : Repository<UserModel>, IUserAccess
{
    protected override string TableName => "users";
    protected override UserModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(UserModel user)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@id", user.Id},
            { "@username", user.Username },
            { "@password", user.Password },
            { "@name", user.Name },
            { "@email", user.Email },
            { "@phone", user.Phone },
            { "@role", user.Role },
            { "@created_at", user.CreatedAt.ToString("yyyy-MM-dd") },
            { "@birth_year", user.BirthYear },
            { "@active", user.Active }
        };

        return parameters;
    }

    public UserAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<UserModel?> GetByUsername(string username)
    {
        var parameters = new Dictionary<string, object> { { "@username", username } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE username = @username", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<UserModel?> GetByEmail(string email)
    {
        var parameters = new Dictionary<string, object> { { "@email", email } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE email = @email", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
