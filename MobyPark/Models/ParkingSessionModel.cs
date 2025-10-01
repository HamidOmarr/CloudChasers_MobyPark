using System.Globalization;
using Microsoft.Data.Sqlite;

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

    public ParkingSessionModel(SqliteDataReader reader)
    {
    Id = reader.IsDBNull(reader.GetOrdinal("id")) ? null : reader.GetInt32(reader.GetOrdinal("id"));
    ParkingLotId = reader.GetInt32(reader.GetOrdinal("parking_lot_id"));
    LicensePlate = reader.GetString(reader.GetOrdinal("licenseplate"));
    Started = DateTime.Parse(reader.GetString(reader.GetOrdinal("started")), null, DateTimeStyles.RoundtripKind);

    Stopped = reader.IsDBNull(reader.GetOrdinal("stopped")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("stopped")), null, DateTimeStyles.RoundtripKind);

    User = reader.GetString(reader.GetOrdinal("user"));
    DurationMinutes = reader.GetInt32(reader.GetOrdinal("duration_minutes"));
    Cost = reader.GetDecimal(reader.GetOrdinal("cost"));
    PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status"));
    }

    public override string ToString() =>
        $"Parking Session [{Id}] for {LicensePlate} at ParkingLot {ParkingLotId}, User: {User}\n" +
        $"Started: {Started}, Stopped: {Stopped}, Duration: {DurationMinutes} mins, Cost: {Cost}, Payment Status: {PaymentStatus}";
}