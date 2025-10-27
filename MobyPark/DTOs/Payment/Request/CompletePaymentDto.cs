using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Payment.Request;

public class CompletePaymentDto : ICanBeEdited
{
    public DateTime? CompletedAt { get; set; }
}
