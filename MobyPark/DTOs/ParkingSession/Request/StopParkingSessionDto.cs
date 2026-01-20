using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Request;

[SwaggerSchema(Description = "Data required to stop a parking session.")]
public class StopParkingSessionDto
{
    [Required]
    [SwaggerSchema("The license plate number (for verification)")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The token representing the payment card to charge")]
    public string CardToken { get; set; } = string.Empty;
}