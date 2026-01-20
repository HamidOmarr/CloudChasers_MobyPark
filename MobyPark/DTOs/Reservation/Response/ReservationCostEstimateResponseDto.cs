using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Reservation.Response;

[SwaggerSchema(Description = "The estimated cost for a proposed reservation time window.")]
public class ReservationCostEstimateResponseDto
{
    [SwaggerSchema("The calculated cost based on the lot's tariff.")]
    public decimal EstimatedCost { get; set; }
}