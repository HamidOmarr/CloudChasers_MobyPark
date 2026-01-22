using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Reservation.Request;

[SwaggerSchema(Description = "Data required to request a cost estimate.")]
public class ReservationCostEstimateRequest
{
    [Required]
    [SwaggerSchema("The parking lot ID to check prices against.")]
    public long ParkingLotId { get; set; }

    [Required]
    [SwaggerSchema("The license plate number (for user ownership verification).")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The proposed start time.")]
    public DateTimeOffset StartDate { get; set; }

    [Required]
    [SwaggerSchema("The proposed end time.")]
    public DateTimeOffset EndDate { get; set; }
}