using MobyPark.Models;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Payment.Response;

[SwaggerSchema(Description = "Details regarding a processed refund.")]
public class RefundResponseDto
{
    [SwaggerSchema("The outcome status of the refund operation.")]
    public string Status { get; set; } = string.Empty;

    [SwaggerSchema("The details of the newly created refund payment record.")]
    public PaymentModel? Refund { get; set; }
}