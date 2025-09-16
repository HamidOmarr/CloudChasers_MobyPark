using System.Globalization;
using Microsoft.Data.Sqlite;

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

    public ReservationModel(SqliteDataReader reader)
    {
        Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader.GetInt32(reader.GetOrdinal("Id"));
        UserId = reader.GetInt32(reader.GetOrdinal("UserId"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("ParkingLotId"));
        VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId"));
        StartTime = DateTime.Parse(
            reader.GetString(reader.GetOrdinal("StartTime")),
            null,
            DateTimeStyles.RoundtripKind // Handles ISO 8601 (e.g. 2025-12-03T11:00:00Z)
        );
        EndTime = DateTime.Parse(
            reader.GetString(reader.GetOrdinal("EndTime")),
            null,
            DateTimeStyles.RoundtripKind
        );
        Status = reader.GetString(reader.GetOrdinal("Status"));
        CreatedAt = DateTime.Parse(
            reader.GetString(reader.GetOrdinal("CreatedAt")),
            null,
            DateTimeStyles.RoundtripKind
        );
        Cost = reader.GetDecimal(reader.GetOrdinal("Cost"));
    }

    public override string ToString() =>
        $"Reservation [{Id}] by User {UserId} for Parking Lot {ParkingLotId} with Vehicle {VehicleId}.\n" +
        $"Start: {StartTime}, End: {EndTime}, Status: {Status}, Cost: {Cost}, Created: {CreatedAt}";
}
