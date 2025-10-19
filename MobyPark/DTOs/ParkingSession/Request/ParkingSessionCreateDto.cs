namespace MobyPark.DTOs.ParkingSession.Request;

public class ParkingSessionCreateDto
{
    public long ParkingLotId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
}