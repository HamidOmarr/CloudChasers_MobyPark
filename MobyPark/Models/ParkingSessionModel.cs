using Npgsql;

namespace MobyPark.Models;

public class ParkingSessionModel
{
    public int? Id { get; set; }
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
        Id = reader.IsDBNull(reader.GetOrdinal("id")) ? null : reader.GetInt32(reader.GetOrdinal("id"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("ParkingLotId"));
        LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate"));
        Started = reader.GetDateTime(reader.GetOrdinal("Started"));
        Stopped = reader.GetFieldValue<DateTime?>(reader.GetOrdinal("Stopped"));
        User = reader.GetString(reader.GetOrdinal("User"));
        DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes"));
        Cost = reader.GetDecimal(reader.GetOrdinal("Cost"));
        PaymentStatus = reader.GetString(reader.GetOrdinal("PaymentStatus"));
    }

    public override string ToString() =>
        $"Parking Session [{Id}] for {LicensePlate} at ParkingLot {ParkingLotId}, User: {User}\n" +
        $"Started: {Started}, Stopped: {Stopped}, Duration: {DurationMinutes} mins, Cost: {Cost}, Payment Status: {PaymentStatus}";
}