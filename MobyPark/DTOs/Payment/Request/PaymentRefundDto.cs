namespace MobyPark.DTOs.Payment.Request;

public record PaymentRefundDto(string PaymentId, decimal? Amount);