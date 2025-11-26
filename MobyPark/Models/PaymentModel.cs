using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class PaymentModel
{
    [Key]
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [ForeignKey(nameof(LicensePlateNumber))]
    public LicensePlateModel LicensePlate { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    [Required]
    public Guid TransactionId { get; set; }

    [ForeignKey(nameof(TransactionId))]
    public TransactionModel Transaction { get; set; } = null!;
}
