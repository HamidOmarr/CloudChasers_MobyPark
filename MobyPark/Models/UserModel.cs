using System.Globalization;
using Npgsql;

namespace MobyPark.Models;

public enum UserRole
{
    Admin = 1,
    ItManager = 2,
    Manager = 3,
    ItEmployee = 4,
    Employee = 5,
    User = 6
}

public class UserModel
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; } = (int)UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateOnly Birthday { get; set; }

    public UserModel() { }

    public UserModel(NpgsqlDataReader reader)
    {
        Id = reader.GetInt64(reader.GetOrdinal("id"));
        Username = reader.GetString(reader.GetOrdinal("username"));
        PasswordHash = reader.GetString(reader.GetOrdinal("password"));
        FirstName = reader.GetString(reader.GetOrdinal("first_name"));
        LastName = reader.GetString(reader.GetOrdinal("last_name"));
        Email = reader.GetString(reader.GetOrdinal("email"));
        Phone = reader.GetString(reader.GetOrdinal("phone"));
        RoleId = reader.GetInt32(reader.GetOrdinal("role_id"));
        var createdStr = reader.GetString(reader.GetOrdinal("created_at"));
        if (!DateTime.TryParse(createdStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var created))
            created = DateTime.UtcNow;
        CreatedAt = created;
        Birthday = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("birthday")));
    }

    public override string ToString() =>
        $"User [{Id}] {FirstName} {LastName} ({Username}), Role ID: {RoleId}, Email: {Email}, Phone: {Phone}, Birthday: {Birthday}, Created At: {CreatedAt}";
}