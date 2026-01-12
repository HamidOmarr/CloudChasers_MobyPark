namespace MobyPark.DTOs.Payment.Request;

public record CreatePaymentDto
{
    public decimal Amount { get; set; }
    public string LicensePlateNumber { get; set; } = string.Empty;
}