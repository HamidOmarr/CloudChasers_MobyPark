using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Payment.Request;

public class CompletePaymentDto : ICanBeEdited
{
    public DateTimeOffset? CompletedAt { get; set; }
}
