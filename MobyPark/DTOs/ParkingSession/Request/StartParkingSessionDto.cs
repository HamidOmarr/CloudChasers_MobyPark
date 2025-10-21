using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.ParkingSession.Request;

public class StartParkingSessionDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public string CardToken { get; set; } = string.Empty;

    public decimal EstimatedAmount { get; set; }

    public bool SimulateInsufficientFunds { get; set; } = false;
}