using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Reservation.Request;

public record CreateReservationDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public long ParkingLotId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? Username { get; set; }
}
