using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingSession.Response;

[SwaggerSchema(Description = "Invoice summary generated upon session stop.")]
public class SessionInvoiceDto
{
    [SwaggerSchema("The unique identifier of the parking session.")]
    public long Id { get; set; }
    [SwaggerSchema("The duration of the parking session in minutes.")]
    public int SessionDuration { get; set; }
    [SwaggerSchema("The total cost incurred during the parking session.")]
    public decimal TotalCost { get; set; }
    [SwaggerSchema("The timestamp when the invoice was created.")]
    public DateTimeOffset CreatedAt { get; set; }
    [SwaggerSchema("The current status of the invoice.")]
    public string Status { get; set; } = string.Empty;
    [SwaggerSchema("A detailed summary of the invoice items.")]
    public List<string> InvoiceSummary { get; set; } = new();
}