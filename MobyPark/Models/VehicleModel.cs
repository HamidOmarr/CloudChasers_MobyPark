using Npgsql;

namespace MobyPark.Models;

public class VehicleModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicensePlate { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public string Color { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }

    public VehicleModel() { }

    public VehicleModel(NpgsqlDataReader reader)
    {
        Id = reader.GetInt32(reader.GetOrdinal("id"));
        UserId = reader.GetInt32(reader.GetOrdinal("user_id"));
        LicensePlate = reader.GetString(reader.GetOrdinal("license_plate"));
        Make = reader.GetString(reader.GetOrdinal("make"));
        Model = reader.GetString(reader.GetOrdinal("model"));
        Color = reader.GetString(reader.GetOrdinal("color"));
        Year = reader.GetInt32(reader.GetOrdinal("year"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
    }

    public override string ToString() =>
        $"Vehicle [{Id}] {Make} {Model} ({LicensePlate}), Color: {Color}, Year: {Year}, OwnerId: {UserId}, Created At: {CreatedAt}";
}