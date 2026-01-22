using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Payment.Request;

[SwaggerSchema(Description = "DTO for updating the completion status of a payment internally.")]
public class CompletePaymentDto : ICanBeEdited
{
    [SwaggerSchema("The timestamp when the payment was successfully completed.")]
    public DateTimeOffset? CompletedAt { get; set; }
}