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
        UserId = reader.GetInt32(reader.GetOrdinal("UserId"));
        LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate"));
        Make = reader.GetString(reader.GetOrdinal("Make"));
        Model = reader.GetString(reader.GetOrdinal("Model"));
        Color = reader.GetString(reader.GetOrdinal("Color"));
        Year = reader.GetInt32(reader.GetOrdinal("Year"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
    }

    public override string ToString() =>
        $"Vehicle [{Id}] {Make} {Model} ({LicensePlate}), Color: {Color}, Year: {Year}, OwnerId: {UserId}, Created At: {CreatedAt}";
}