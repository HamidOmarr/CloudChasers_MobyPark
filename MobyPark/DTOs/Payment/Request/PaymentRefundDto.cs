using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Payment.Request;

[SwaggerSchema(Description = "Data required to process a refund.")]
public class PaymentRefundDto
{
    [Required]
    [SwaggerSchema("The unique identifier of the payment to be refunded.")]
    public string PaymentId { get; set; } = string.Empty;

    [SwaggerSchema("The amount to refund. If null, may default to full amount.")]
    public decimal? Amount { get; set; }
}