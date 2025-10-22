using System.ComponentModel.DataAnnotations;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class TransactionModel : ICanBeEdited
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
