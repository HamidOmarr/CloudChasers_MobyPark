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
        UserId = reader.GetInt32(reader.GetOrdinal("user_id"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("parking_lot_id"));
        VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id"));
        StartTime = reader.GetDateTime(reader.GetOrdinal("start_time"));
        EndTime = reader.GetDateTime(reader.GetOrdinal("end_time"));
        Status = reader.GetString(reader.GetOrdinal("status"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
        Cost = (decimal)reader.GetFloat(reader.GetOrdinal("cost"));
    }

    public override string ToString() =>
        $"Reservation [{Id}] by User {UserId} for Parking Lot {ParkingLotId} with Vehicle {VehicleId}.\n" +
        $"Start: {StartTime}, End: {EndTime}, Status: {Status}, Cost: {Cost}, Created: {CreatedAt}";
}
