using Swashbuckle.AspNetCore.Annotations;
using MobyPark.Models;

namespace MobyPark.DTOs.Reservation.Response;

[SwaggerSchema(Description = "The result of a reservation creation request.")]
public class ReservationResponseDto
{
    [SwaggerSchema("The outcome status of the operation.")]
    public string Status { get; set; } = string.Empty;

    [SwaggerSchema("The full reservation details.")]
    public ReservationModel? Reservation { get; set; }
}