using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Request;

[SwaggerSchema(Description = "Data for manual updates to a session (Admin).")]
public class UpdateParkingSessionDto : ICanBeEdited
{
    [SwaggerSchema("The timestamp when the session was stopped")]
    public DateTimeOffset? Stopped { get; set; }
    [SwaggerSchema("The status of the payment for the session")]
    public ParkingSessionStatus? PaymentStatus { get; set; }
    [SwaggerSchema("The total cost of the parking session")]
    public decimal? Cost { get; set; }
}