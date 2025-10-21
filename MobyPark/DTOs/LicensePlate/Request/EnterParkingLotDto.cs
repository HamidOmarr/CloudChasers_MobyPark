namespace MobyPark.DTOs.LicensePlate.Request;

public class EnterParkingLotDto
{
    public string LicensePlate { get; set; } = string.Empty;
    public long ParkingLotId { get; set; }
}

