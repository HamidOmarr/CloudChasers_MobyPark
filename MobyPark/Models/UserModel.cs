using Npgsql;

namespace MobyPark.Models;

public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; } // stored hashed
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BirthYear { get; set; }
    public bool Active { get; set; }

    public UserModel()
    {
        Role = "USER";
    }

    public UserModel(NpgsqlDataReader reader)
    {
        Id = reader.GetInt32(reader.GetOrdinal("id"));
        Username = reader.GetString(reader.GetOrdinal("Username"));
        Password = reader.GetString(reader.GetOrdinal("Password"));
        Name = reader.GetString(reader.GetOrdinal("Name"));
        Email = reader.GetString(reader.GetOrdinal("Email"));
        Phone = reader.GetString(reader.GetOrdinal("Phone"));
        Role = reader.GetString(reader.GetOrdinal("Role"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
        BirthYear = reader.GetInt32(reader.GetOrdinal("BirthYear"));
        Active = reader.GetBoolean(reader.GetOrdinal("Active"));
    }

    public override string ToString() =>
        $"User [{Id}] {Name} ({Username}), Role: {Role}, Email: {Email}, Phone: {Phone}, Birth Year: {BirthYear}, Active: {Active}, Created At: {CreatedAt}";
}