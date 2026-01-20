
using System.ComponentModel.DataAnnotations;

using MobyPark.Models;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Invoice;

[SwaggerSchema(Description = "Data for updating an invoice.")]
public class UpdateInvoiceDto
{
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