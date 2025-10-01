using System.Globalization;
using Microsoft.Data.Sqlite;

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


    public UserModel(SqliteDataReader reader)
    {
        Id = reader.GetInt32(reader.GetOrdinal("id"));
        Username = reader.GetString(reader.GetOrdinal("username"));
        Password = reader.GetString(reader.GetOrdinal("password"));
        Name = reader.GetString(reader.GetOrdinal("name"));
        Email = reader.GetString(reader.GetOrdinal("email"));
        Phone = reader.GetString(reader.GetOrdinal("phone"));
        Role = reader.GetString(reader.GetOrdinal("role"));
        CreatedAt = DateTime.ParseExact(reader.GetString(reader.GetOrdinal("created_at")), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        BirthYear = reader.GetInt32(reader.GetOrdinal("birth_year"));
        Active = reader.GetBoolean(reader.GetOrdinal("active"));
    }

    public override string ToString() =>
        $"User [{Id}] {Name} ({Username}), Role: {Role}, Email: {Email}, Phone: {Phone}, Birth Year: {BirthYear}, Active: {Active}, Created At: {CreatedAt}";
}