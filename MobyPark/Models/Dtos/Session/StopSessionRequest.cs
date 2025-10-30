using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests.Session;

public class StopSessionRequest
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public PaymentValidationRequest PaymentValidation { get; set; }
}