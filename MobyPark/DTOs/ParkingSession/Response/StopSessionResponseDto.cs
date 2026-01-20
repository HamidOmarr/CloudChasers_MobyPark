using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Response;

[SwaggerSchema(Description = "Response details when a session is stopped.")]
public class StopSessionResponseDto
{
    [SwaggerSchema("The status of the parking session after stopping.")]
    public string Status { get; set; } = "Stopped";
    [SwaggerSchema("The unique identifier of the parking session.")]
    public long SessionId { get; set; }
    [SwaggerSchema("The license plate associated with the parking session.")]
    public string LicensePlate { get; set; } = string.Empty;
    [SwaggerSchema("The unique identifier of the parking lot where the session took place.")]
    public long ParkingLotId { get; set; }
    [SwaggerSchema("The timestamp when the parking session started.")]
    public DateTimeOffset StartedAt { get; set; }
    [SwaggerSchema("The timestamp when the parking session was stopped.")]
    public DateTimeOffset? StoppedAt { get; set; }
    [SwaggerSchema("The total amount charged for the parking session.")]
    public string PaymentStatus { get; set; } = string.Empty;
    [SwaggerSchema("The invoice details for the stopped parking session.")]
    public SessionInvoiceDto Invoice { get; set; } = new();
}