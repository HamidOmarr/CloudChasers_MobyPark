
using System.ComponentModel.DataAnnotations;

using MobyPark.Models;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Invoice;

[SwaggerSchema(Description = "Data required to create an invoice.")]
public class CreateInvoiceDto
{
    [Required]
    [SwaggerSchema("License plate associated with the invoice")]
    public string LicensePlateId { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("Parking session associated with the invoice")]
    public long ParkingSessionId { get; set; }

    [Required]
    [SwaggerSchema("Duration of the parking session in minutes")]
    public int SessionDuration { get; set; }
    [Required]
    [SwaggerSchema("Cost of the parking session")]
    public decimal Cost { get; set; }

    [Required]
    [SwaggerSchema("Status of the invoice")]
    public InvoiceStatus Status { get; set; }
}