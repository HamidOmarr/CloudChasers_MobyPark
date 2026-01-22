using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Reservation.Request;

[SwaggerSchema(Description = "Data required to create a reservation.")]
public class CreateReservationDto
{
    [Required]
    [SwaggerSchema("The unique identifier of the parking lot.")]
    public long ParkingLotId { get; set; }

    [Required]
    [SwaggerSchema("The license plate to reserve the spot for.")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The start date and time of the reservation.")]
    public DateTimeOffset StartDate { get; set; }

    [Required]
    [SwaggerSchema("The end date and time of the reservation.")]
    public DateTimeOffset EndDate { get; set; }

    [SwaggerSchema("Optional: The username to create the reservation for (Admin only).")]
    public string? Username { get; set; }
}