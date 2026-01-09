namespace MobyPark.DTOs.Invoice;

public class InvoiceResponseDto
{
    public long Id { get; set; }

    public string LicensePlate { get; set; } = string.Empty;

    public string Started { get; set; } = string.Empty;
    public string Stopped { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TotalCost { get; set; } = string.Empty;

    public List<string> InvoiceSummary { get; set; } = new();
}
