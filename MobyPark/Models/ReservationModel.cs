using Npgsql;

namespace MobyPark.Models;

public class ReservationModel
{
    public int? Id { get; set; }
    public int UserId { get; set; }
    public int ParkingLotId { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Cost { get; set; }

    public ReservationModel() { }

    public ReservationModel(NpgsqlDataReader reader)
    {
        Id = reader.GetFieldValue<int?>(reader.GetOrdinal("id"));
        UserId = reader.GetInt32(reader.GetOrdinal("UserId"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("ParkingLotId"));
        VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId"));
        StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));
        EndTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
        Status = reader.GetString(reader.GetOrdinal("Status"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
        Cost = reader.GetDecimal(reader.GetOrdinal("Cost"));
    }

    public override string ToString() =>
        $"Reservation [{Id}] by User {UserId} for Parking Lot {ParkingLotId} with Vehicle {VehicleId}.\n" +
        $"Start: {StartTime}, End: {EndTime}, Status: {Status}, Cost: {Cost}, Created: {CreatedAt}";
}
