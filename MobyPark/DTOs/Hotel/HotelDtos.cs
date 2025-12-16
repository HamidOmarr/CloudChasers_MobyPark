using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Hotel;

public class CreateHotelDto
{
    [Required]
    public string Name { get; set; }
    [Required]
    public string Address { get; set; }
    [Required]
    public string IBAN { get; set; }
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
    public string Name { get; set; }
    public string Address { get; set; }
    public long HotelParkingLotId { get; set; }
}
