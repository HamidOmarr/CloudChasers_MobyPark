using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models;

public class TransactionModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Method { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Bank { get; set; } = string.Empty;
}
