using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;
using NpgsqlTypes;

namespace MobyPark.Models;

public enum ParkingLotStatus
{
    [PgName("open")]
    Open,
    [PgName("closed")]
    Closed,
    [PgName("maintenance")]
    Maintenance
}

public class ParkingLotModel : IHasLongId, ICanBeEdited
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public int Capacity { get; set; } = 0;

    [Required]
    public int Reserved { get; set; } = 0;

    [NotMapped]
    public int AvailableSpots => Capacity - Reserved;

    [Required]
    public decimal Tariff { get; set; } = 0m;

    public decimal? DayTariff { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public ParkingLotStatus Status { get; set; } = ParkingLotStatus.Open;

    [Required]
    public ICollection<ReservationModel> Reservations { get; set; } = new List<ReservationModel>();
}
