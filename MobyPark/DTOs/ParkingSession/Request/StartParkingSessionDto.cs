using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Request;

[SwaggerSchema(Description = "Data required to start a parking session.")]
public class StartParkingSessionDto
{
    [Required]
    [SwaggerSchema("The license plate number to park")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The token representing the payment card")]
    public string CardToken { get; set; } = string.Empty;

    [SwaggerSchema("The amount to pre-authorize on the card")]
    public decimal EstimatedAmount { get; set; }

    [SwaggerSchema("For testing: Simulate a card decline")]
    public bool SimulateInsufficientFunds { get; set; } = false;
}