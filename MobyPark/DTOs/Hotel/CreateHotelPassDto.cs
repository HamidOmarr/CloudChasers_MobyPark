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
}

public class ReadHotelPassDto
{
    [Required]
    public long Id { get; set; }
    [Required]
    public string LicensePlate { get; set; }
    [Required]
    public int ParkingLotId { get; set; }
    [Required]
    public DateTime Start { get; set; }
    [Required]
    public DateTime End { get; set; }
}

public class PatchHotelPassDto
{
    public string? LicensePlate { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}