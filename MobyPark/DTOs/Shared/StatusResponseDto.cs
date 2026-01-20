using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Shared;

[SwaggerSchema(Description = "Standard response for status updates or simple messages.")]
public class StatusResponseDto
{
    [SwaggerSchema("The short status of the operation (e.g., 'Deleted', 'Success').")]
    public string? Status { get; set; }

    [SwaggerSchema("A descriptive message regarding the operation result.")]
    public string? Message { get; set; }
}