using System.ComponentModel.DataAnnotations;
using MobyPark.Models;

namespace MobyPark.DTOs.Invoice;

public class UpdateInvoiceDto
{
    [Required]
    public string LicensePlateId { get; set; } = string.Empty;
    [Required]
    public long ParkingSessionId { get; set; }

    [Required]
    public DateTimeOffset Started { get; set; }

    [Required]
    public DateTimeOffset Stopped { get; set; }

    [Required]
    public decimal? Cost { get; set; }

    public InvoiceStatus Status { get; set; }

    public List<string> InvoiceSummary { get; set; } = new List<string>();
}