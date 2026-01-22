using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Response;

[SwaggerSchema(Description = "Response details when a session is successfully started.")]
public class StartSessionResponseDto
{
    [SwaggerSchema("The status of the parking session")]
    public string Status { get; set; } = "Started";
    [SwaggerSchema("The unique identifier of the parking session")]
    public long SessionId { get; set; }
    [SwaggerSchema("The license plate associated with the parking session")]
    public string LicensePlate { get; set; } = string.Empty;
    [SwaggerSchema("The unique identifier of the parking lot")]
    public long ParkingLotId { get; set; }
    [SwaggerSchema("The timestamp when the parking session started")]
    public DateTimeOffset StartedAt { get; set; }
    [SwaggerSchema("The payment status for the parking session")]
    public string PaymentStatus { get; set; } = string.Empty;
    [SwaggerSchema("Remaining spots in the lot after this session started")]
    public int AvailableSpots { get; set; }
}