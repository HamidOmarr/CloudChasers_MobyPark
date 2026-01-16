
using System.ComponentModel.DataAnnotations;

using MobyPark.Models;
namespace MobyPark.DTOs.Invoice;

public class UpdateInvoiceDto
{
    [Required]
    public int SessionDuration { get; set; }
    [Required]
    public decimal Cost { get; set; }

    [Required]
    public InvoiceStatus Status { get; set; }
}