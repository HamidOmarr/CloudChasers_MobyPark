namespace MobyPark.DTOs.Payment.Request;

public record CreatePaymentDto
{
    public decimal Amount { get; set; } = 0.0m;
    public string LicensePlateNumber { get; set; } = string.Empty;
}
