using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.ParkingSession.Request;

public class StopParkingSessionDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public string CardToken { get; set; } = string.Empty;




}