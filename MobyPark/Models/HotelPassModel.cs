using System.ComponentModel.DataAnnotations.Schema;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class HotelPassModel : IHasLongId
{
    public long Id { get; set; }
    public long ParkingLotId { get; set; }
    [ForeignKey(nameof(ParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; } = null!;
    public string LicensePlateNumber { get; set; } = string.Empty;
    [ForeignKey(nameof(LicensePlateNumber))]
    public LicensePlateModel LicensePlate { get; set; } = null!;

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public TimeSpan ExtraTime { get; set; }
}