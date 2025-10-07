using Npgsql;

namespace MobyPark.Models;

public class ParkingSessionModel
{
    public int Id { get; set; }
    public int ParkingLotId { get; set; }
    public string LicensePlate { get; set; }
    public DateTime Started { get; set; }
    public DateTime? Stopped { get; set; }
    public string User { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Cost { get; set; }
    public string PaymentStatus { get; set; }

    public ParkingSessionModel() { }

    public ParkingSessionModel(NpgsqlDataReader reader)
    {
        Id = reader.GetInt32(reader.GetOrdinal("id"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("parking_lot_id"));
        LicensePlate = reader.GetString(reader.GetOrdinal("license_plate"));
        Started = reader.GetDateTime(reader.GetOrdinal("started"));
        Stopped = reader.GetFieldValue<DateTime?>(reader.GetOrdinal("stopped"));
        User = reader.GetString(reader.GetOrdinal("user_name"));
        DurationMinutes = reader.GetInt32(reader.GetOrdinal("duration_minutes"));
        Cost = (decimal)reader.GetFloat(reader.GetOrdinal("cost"));
        PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status"));
    }

    public override string ToString() =>
        $"Parking Session [{Id}] for {LicensePlate} at ParkingLot {ParkingLotId}, User: {User}\n" +
        $"Started: {Started}, Stopped: {Stopped}, Duration: {DurationMinutes} mins, Cost: {Cost}, Payment Status: {PaymentStatus}";
}