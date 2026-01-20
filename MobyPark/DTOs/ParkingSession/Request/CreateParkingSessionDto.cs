using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Request;

[SwaggerSchema(Description = "Internal DTO for session creation logic.")]
public class CreateParkingSessionDto
{
    [SwaggerSchema("The ID of the parking lot where the session is started.")]
    public long ParkingLotId { get; set; }
    [SwaggerSchema("The license plate of the vehicle.")]
    public string LicensePlate { get; set; } = string.Empty;
    [SwaggerSchema("The timestamp when the parking session started.")]
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;
}