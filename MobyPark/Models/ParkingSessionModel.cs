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
        Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader.GetInt32(reader.GetOrdinal("Id"));
        ParkingLotId = reader.GetInt32(reader.GetOrdinal("ParkingLotId"));
        LicensePlate = reader.GetString(reader.GetOrdinal("LicensePlate"));
        Started = DateTime.Parse(reader.GetString(reader.GetOrdinal("Started")), null, DateTimeStyles.RoundtripKind);

        Stopped = reader.IsDBNull(reader.GetOrdinal("Stopped")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("Stopped")), null, DateTimeStyles.RoundtripKind);

        User = reader.GetString(reader.GetOrdinal("User"));
        DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes"));
        Cost = reader.GetDecimal(reader.GetOrdinal("Cost"));
        PaymentStatus = reader.GetString(reader.GetOrdinal("PaymentStatus"));
    }

    public override string ToString() =>
        $"Parking Session [{Id}] for {LicensePlate} at ParkingLot {ParkingLotId}, User: {User}\n" +
        $"Started: {Started}, Stopped: {Stopped}, Duration: {DurationMinutes} mins, Cost: {Cost}, Payment Status: {PaymentStatus}";
}