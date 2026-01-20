using MobyPark.Models;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Payment.Response;

[SwaggerSchema(Description = "Wraps the payment model with a status message.")]
public class PaymentResponseDto
{
    [SwaggerSchema("The outcome status of the operation (e.g., 'Success', 'Created').")]
    public string Status { get; set; } = string.Empty;

    [SwaggerSchema("The full details of the payment record.")]
    public PaymentModel? Payment { get; set; }
}