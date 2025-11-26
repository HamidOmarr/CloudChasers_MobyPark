namespace MobyPark.DTOs.ParkingSession.Request;

public class CreateParkingSessionDto
{
    public long ParkingLotId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;
}