using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public enum ParkingSessionStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public class ParkingSessionModel : IHasLongId
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
    public DateTime Started { get; set; } = DateTime.UtcNow;

    public DateTime? Stopped { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? Cost { get; set; }

    [Required]
    public ParkingSessionStatus PaymentStatus { get; set; } = ParkingSessionStatus.Pending;
}