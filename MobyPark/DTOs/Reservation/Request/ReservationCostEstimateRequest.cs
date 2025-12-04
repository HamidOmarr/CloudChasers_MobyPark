using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Reservation.Request;

public class ReservationCostEstimateRequest
{
    [Required]
    public long ParkingLotId { get; set; }

    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset StartDate { get; set; }

    [Required]
    public DateTimeOffset EndDate { get; set; }
}
