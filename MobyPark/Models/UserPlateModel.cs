using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class UserPlateModel : IHasLongId, ICanBeEdited
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; } = null!;

    [Required]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [ForeignKey(nameof(LicensePlateNumber))]
    public LicensePlateModel LicensePlate { get; set; } = null!;

    [Required]
    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const long DefaultUserId = -1;  // Defaults to deleted (generic) user
}