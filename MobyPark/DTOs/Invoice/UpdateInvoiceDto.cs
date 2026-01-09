using System.ComponentModel.DataAnnotations;
using MobyPark.Models;

namespace MobyPark.DTOs.Invoice;

public class UpdateInvoiceDto
{
    [Required]
    public DateTimeOffset Started { get; set; }

    [Required]
    public DateTimeOffset Stopped { get; set; }

    [Required]
    public decimal Cost { get; set; }

    [Required]
    public InvoiceStatus Status { get; set; }
}
