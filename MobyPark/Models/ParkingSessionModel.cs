using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;
using NpgsqlTypes;

namespace MobyPark.Models;

public enum ParkingSessionStatus
{
    [PgName("preauthorized")]
    PreAuthorized,
    [PgName("pending")]
    Pending,
    [PgName("paid")]
    Paid,
    [PgName("failed")]
    Failed,
    [PgName("refunded")]
    Refunded
}

public class ParkingSessionModel : IHasLongId, ICanBeEdited
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ParkingLotId { get; set; }

    [ForeignKey(nameof(ParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; } = null!;

    [Required]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [ForeignKey(nameof(LicensePlateNumber))]
    public LicensePlateModel LicensePlate { get; set; } = null!;

    [Required]
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? Stopped { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? Cost { get; set; }

    [Required]
    public ParkingSessionStatus PaymentStatus { get; set; } = ParkingSessionStatus.Pending;
}