using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Hotel;

public class CreateHotelPassDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;
    [Required]
    public DateTimeOffset Start { get; set; }
    [Required]
    public DateTimeOffset End { get; set; }

    public TimeSpan ExtraTime { get; set; } = new(0, 30, 0);
}

public class AdminCreateHotelPassDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;
    [Required]
    public long ParkingLotId { get; set; }
    [Required]
    public DateTimeOffset Start { get; set; }
    [Required]
    public DateTimeOffset End { get; set; }

    public TimeSpan ExtraTime { get; set; } = new(0, 30, 0);
}

public class ReadHotelPassDto
{
    [Required]
    public long Id { get; set; }
    [Required]
    public string LicensePlate { get; set; } = string.Empty;
    [Required]
    public long ParkingLotId { get; set; }
    [Required]
    public DateTimeOffset Start { get; set; }
    [Required]
    public DateTimeOffset End { get; set; }
    [Required]
    public TimeSpan ExtraTime { get; set; }
}

public class PatchHotelPassDto
{
    [Required]
    public long Id { get; set; }
    public string? LicensePlate { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public TimeSpan? ExtraTime { get; set; }
}