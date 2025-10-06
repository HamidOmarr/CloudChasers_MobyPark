using System.Globalization;
using Npgsql;

namespace MobyPark.Models;

public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int BirthYear { get; set; }
    public bool Active { get; set; } = true;

    public UserModel()
    {
        Role = "USER";
    }

    public UserModel(NpgsqlDataReader reader)
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id"));
        Username = reader.GetString(reader.GetOrdinal("Username"));
        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
        Name = reader.GetString(reader.GetOrdinal("Name"));
        Email = reader.GetString(reader.GetOrdinal("Email"));
        Phone = reader.GetString(reader.GetOrdinal("Phone"));
        Role = reader.GetString(reader.GetOrdinal("Role"));
        var createdStr = reader.GetString(reader.GetOrdinal("CreatedAt"));
        if (!DateTime.TryParse(createdStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var created))
            created = DateTime.UtcNow;
        CreatedAt = created;
        BirthYear = reader.GetInt32(reader.GetOrdinal("BirthYear"));
        var activeOrdinal = reader.GetOrdinal("Active");
        Active = reader.GetFieldType(activeOrdinal) == typeof(bool)
            ? reader.GetBoolean(activeOrdinal)
            : Convert.ToInt32(reader.GetValue(activeOrdinal)) != 0;
    }
    
    public override string ToString() =>
        $"User [{Id}] {Name} ({Username}), Role: {Role}, Email: {Email}, Phone: {Phone}, Birth Year: {BirthYear}, Active: {Active}, Created At: {CreatedAt}";
}