using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class UserAccess : Repository<UserModel>, IUserAccess
{
    protected override string TableName => "users";
    protected override UserModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(UserModel user)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@Username", user.Username },
            { "@Password", user.PasswordHash },
            { "@Name", user.Name },
            { "@Email", user.Email },
            { "@Phone", user.Phone },
            { "@Role", user.Role },
            { "@Created_At", user.CreatedAt.ToString("yyyy-MM-dd") },
            { "@Birth_Year", user.BirthYear },
            { "@Active", user.Active }
        };

        return parameters;
    }

    public UserAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<UserModel?> GetByUsername(string username)
    {
        var parameters = new Dictionary<string, object> { { "@Username", username } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE username = @Username", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

    public async Task<UserModel?> GetByEmail(string email)
    {
        var parameters = new Dictionary<string, object> { { "@Email", email } };
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE email = @Email", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }
}
