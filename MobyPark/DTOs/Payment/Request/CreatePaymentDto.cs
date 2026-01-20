using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Payment.Request;

[SwaggerSchema(Description = "Data required to initiate a new payment.")]
public class CreatePaymentDto
{
    [Required]
    [SwaggerSchema("The monetary amount to be charged.")]
    public decimal Amount { get; set; }

    [Required]
    [SwaggerSchema("The license plate number associated with the payment.")]
    public string LicensePlateNumber { get; set; } = string.Empty;
}