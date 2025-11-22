using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Hotel;

public class CreateHotelPassDto
{
    [Required]
    public string LicensePlate { get; set; }
    [Required]
    public int ParkingLotId { get; set; }
    [Required]
    public DateTime Start { get; set; }
    [Required]
    public DateTime End { get; set; }

    public TimeSpan ExtraTime { get; set; } = new TimeSpan(0, 30, 0);
}

public class ReadHotelPassDto
{
    public long Id { get; set; }
    public string LicensePlate { get; set; }
    public int ParkingLotId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TimeSpan ExtraTime { get; set; }
}

public class PatchHotelPassDto
{
    public string? LicensePlate { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? ExtraTime { get; set; }
}