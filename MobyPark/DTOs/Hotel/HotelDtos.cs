using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Hotel;

public class CreateHotelDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Address { get; set; } = string.Empty;
    [Required]
    public string IBAN { get; set; } = string.Empty;
    [Required]
    public long HotelParkingLotId { get; set; }
}

public class PatchHotelDto
{
    [Required]
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? IBAN { get; set; }
    public long? HotelParkingLotId { get; set; }
}

public class ReadHotelDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public long HotelParkingLotId { get; set; }
}