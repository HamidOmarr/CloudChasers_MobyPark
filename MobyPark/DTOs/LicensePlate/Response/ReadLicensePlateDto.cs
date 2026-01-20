using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.LicensePlate.Response;

[SwaggerSchema(Description = "Details of a registered license plate.")]
public class ReadLicensePlateDto
{
    [SwaggerSchema("The license plate number")]
    public string LicensePlate { get; set; } = string.Empty;

    [SwaggerSchema("The ID of the user who owns this plate")]
    public long UserId { get; set; }

    [SwaggerSchema("Optional status report relating to the method this plate was retrieved")]
    public string? Status { get; set; }
}