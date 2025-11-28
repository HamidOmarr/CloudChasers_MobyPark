using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;
using NpgsqlTypes;

namespace MobyPark.Models;

public enum ReservationStatus
{
    [PgName("pending")]
    Pending,
    [PgName("confirmed")]
    Confirmed,
    [PgName("cancelled")]
    Cancelled,
    [PgName("completed")]
    Completed,
    [PgName("no_show")]
    NoShow
}

public class ReservationModel : IHasLongId, ICanBeEdited
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [ForeignKey(nameof(LicensePlateNumber))]
    public LicensePlateModel LicensePlate { get; set; } = null!;

    [Required]
    public long ParkingLotId { get; set; }

    [ForeignKey(nameof(ParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; } = null!;

    [Required]
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public decimal Cost { get; set; } = 0m;
}
